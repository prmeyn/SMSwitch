# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build SMSwitch.sln --configuration Release      # must stay at 0 warnings
dotnet test SMSwitch.sln --configuration Release
dotnet format whitespace SMSwitch.sln                  # applies .editorconfig
dotnet pack --configuration Release --no-build --output .   # only SMSwitch is packable
```

Run one test class or one test:

```bash
dotnet test SMSwitch.Tests/SMSwitch.Tests.csproj --filter "FullyQualifiedName~ProviderFailoverTests"
dotnet test SMSwitch.Tests/SMSwitch.Tests.csproj --filter "FullyQualifiedName~ProviderFailoverTests.A_failing_provider_falls_through_to_the_next"
```

`TestAPIs` is a local harness, not a deployable service. Its endpoints are mapped only in the
`Development` environment because they have no authentication and send real SMS. `TestAPIs.http`
has ready-made requests.

## What this is

A NuGet library that fronts multiple SMS providers for OTP send/verify and plain SMS, choosing a
provider per destination country and failing over. It is on the authentication path and spends real
money per message, so correctness and concurrency matter more than the line count suggests.

Three layers: `SMSwitchService` (the switchboard) → provider services implementing
`IServiceMobileNumbers` (Twilio, Plivo, DevConsole) → the provider SDKs. Providers are resolved by
keyed DI on the `SmsProvider` enum (`ServiceCollectionExtensions`), so adding a provider is a
registration plus an implementation, not a new `switch` arm.

`IServiceMobileNumbers` is the *single-provider* contract. It has no resend cooldown, because
deduplicating repeated sends is the switchboard's job; providers only take
`deliveryConfirmationTimeoutInSeconds`. `SMSwitchService` takes both durations and implements the
interface explicitly.

## The provider queue is the session state machine

This is the central idea and it spans `ProviderFailover`, `SMSwitchService` and the session DTOs.

- `ProviderFailover.BuildQueue` builds a queue from the configured priority for the country phone
  code (falling back to `FallBackPriority`), repeated `MaxRoundRobinAttempts` times.
- `TrySendThroughQueue` works through it until a provider succeeds, and **deliberately leaves the
  successful provider at the head**, because `VerifyOTP` peeks the head to route verification back
  to whichever provider actually sent the OTP.
- On a resend the head is the already-spent provider, so it is discarded first.
- **An empty queue means the session is dead.** `HasNotExpired` on both session DTOs treats an empty
  (but not null) queue as expired. A null queue is a fresh session.

Consequence: never "tidy up" the queue by dequeuing after a successful send, and never rebuild it
when the session already has one.

## Persistence constraints

These are all non-obvious and have caused real bugs:

- **`DateTimeOffset` is stored as a document** `{ DateTime, Ticks, Offset }`, not a BSON date. Range
  filters and sorts work only because MongoDB compares subfields in order and `DateTime` is first. A
  TTL index must therefore target `ExpiryTimeUTC.DateTime`, not `ExpiryTimeUTC`.
- **`SmsProvider` values are persisted by number** inside `SmsProvidersQueue`. Append new members;
  renumbering or inserting reinterprets every session already in the database.
- **Mutate server-side, never read-modify-write.** `FailedVerificationAttemptsDateTimeOffset` is a
  brute-force limiter; a read, mutate, replace cycle loses increments under concurrency and the real
  ceiling becomes request concurrency rather than `MaximumFailedAttemptsToVerify`. Use `$push` and
  filtered `FindOneAndUpdate` (see `SMSwitchDbService.RecordFailedVerificationAttempt` /
  `RecordSuccessfulVerification`).
- **`_id` cannot be updated.** `SMSwitchSendSMSSession` has a surrogate `Guid` `_id` with the
  recipient-and-message hash in an indexed `DedupeKey` field, because keying the document by that
  hash made a repeated message permanently undeliverable.
- **`CountryInfo.ValidLengthsAndFormat` is null, not empty**, for a country with no known lengths, so
  a plain `$set` on the dotted path fails against BSON null. `FeedbackAsync` uses a pipeline update
  with `$ifNull`, which needs **MongoDB 4.2+**.

Indexes are created in `StartAsync` on hosted services (`SMSwitchDbService`, `PlivoDbService`,
`CountryDbService`), not fire-and-forget from a constructor. `PlivoDbService` is hosted *only* to
create its expiry index — it works fine without being hosted, which is exactly why the registration
is easy to drop, so a test guards it.

Retention is `Controls:SessionRetentionDays`, default 30, applied to `SMSwitchSession`,
`SMSwitchSendSMSSession` and `PlivoSession`. All three go through `Database/TtlIndexes.cs`, which
exists because these indexes are created from `StartAsync`: an unhandled `IndexOptionsConflict` (85)
or `IndexKeySpecsConflict` (86) there does not degrade a query, it stops the application booting. So
changing the setting is applied with `collMod` against the index's *looked-up* name, and a
non-positive value **drops** the index rather than merely skipping creation — otherwise disabling
retention would silently do nothing on any deployment that already had one. The matcher requires a
single-key index so it cannot amend or drop one of the lookup indexes.

## Configuration

`SMSwitchGeneralInitializer` reads the shared `SMSwitchSettings` block; each provider has its own
initializer deriving from it. Two deliberate failure modes:

- **Fail hard at startup** for a missing required section (`SupportedCountriesIsoCodes`,
  `Controls:PriorityBasedOnCountryPhoneCode`, `Controls:FallBackPriority`) or an unparseable country
  code.
- **Fail soft** for a provider: unknown provider names are dropped with a logged warning and that
  country falls back; missing credentials leave the provider's settings field null and disable it.
  A provider initializer must therefore assign its settings field **only after** validation and
  client construction succeed, or the null-guards in the service are defeated.

Priority lists are `List<SmsProvider>`, not `HashSet` — they are ordered and repeats are meaningful.

## Provider differences worth knowing

- `DevConsoleService` refuses to operate in `Production` and reports the send as failed, so the
  queue falls through to a real provider.
- `PlivoDbService.KeepCheckingTheDatabaseIfSentEvery2seconds` returns true immediately outside
  `Production`, because the delivery webhook cannot reach a dev machine.
- Neither the Twilio nor the Plivo SDK accepts a `CancellationToken`, and several Plivo calls are
  synchronous. Tokens reach the Mongo calls and the `Task.Delay` polling loops only.
- `OtpLength` is pushed to the Twilio Verify *service* at startup — account-wide configuration, not
  a per-request option. Plivo's length is fixed at 6.

The Plivo webhook verifies Plivo's `X-Plivo-Signature-V3` against `PlivoInitializer.NotificationUrl`
plus the received query string — **the URL this app gave Plivo**, not one rebuilt from the incoming
request. That is deliberate: it survives a proxy rewriting scheme or host. It fails closed.

## Tests

`SMSwitch.Tests` (xUnit) runs without a MongoDB instance or provider credentials, and should stay
that way. It covers pure logic: `MobileNumber` parsing, `HasNotExpired`, the configuration binder,
the failover queue, webhook signature verification, and the DI registrations.

`InternalsVisibleTo` is set, so internal types are testable. When logic is worth testing, extract it
to a static helper with no database or provider dependency, as `ProviderFailover` and
`SupportedLocales` were.

`ExternalPackageAssumptionsTests` pins behaviour of the companion packages that would otherwise fail
silently on an upgrade. Add to it rather than assuming.

## Companion packages

`EarthCountriesInfo`, `HumanLanguages`, `MongoDbService`, `MongoDbTokenManager`,
`uSignIn.CommonSettings` and `Meyn.Utilities` are all authored at https://github.com/prmeyn — read
the source there instead of inferring behaviour from names.

Note that the `SMSwitch.Countries` namespace shadows `EarthCountriesInfo.Countries`, so that class
must be fully qualified.

## Style and release

`.editorconfig` is authoritative: tabs for C#, `utf-8-bom` for `.cs` (matching what Visual Studio
writes), and `end_of_line` deliberately unset so formatting does not rewrite line endings in a
repository that stores LF and checks out CRLF.

Releases are tag-driven: pushing a `v*.*.*` tag builds, tests and publishes to NuGet via
`.github/workflows/release.yml`. The version comes from the tag, not from the csproj. Actions are
pinned to commit SHAs because that job holds an id-token that can publish.

Licensed **MIT**, like the other packages in the same org.
