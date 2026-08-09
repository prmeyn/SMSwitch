# SMSwitch

[![NuGet](https://img.shields.io/nuget/v/SMSwitch.svg)](https://www.nuget.org/packages/SMSwitch)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SMSwitch.svg)](https://www.nuget.org/packages/SMSwitch)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**SMSwitch** is an open-source C# class library that acts as a switchboard in front of multiple SMS providers. It sends one-time passwords (OTPs) and plain SMS messages through **Twilio** or **Plivo**, choosing the provider per destination country and automatically failing over to the next provider when one fails. All sessions and attempts are stored in your own MongoDB instance for auditing.

## Features

- **Multi-provider with failover** — configure a provider priority per country phone code plus a global fallback; failed sends automatically retry with the next provider in the queue.
- **OTP send & verify** — delegates to the providers' hosted verification products (Twilio Verify, Plivo Verify), including localized OTP message templates.
- **Plain SMS** — with delivery-status confirmation and resend-cooldown deduplication.
- **`DevConsole` provider for local testing** — prints the OTP to the console instead of sending a real SMS ([see below](#local-testing-without-real-sms)).
- **Audit trail in your own MongoDB** — every session, attempt, and delivery notification is stored in your database.
- **Android SMS Retriever support** — pass your app hash so OTP messages can be auto-read on Android.

## How it works

For each phone number, SMSwitch builds a queue of providers from your configured priorities (`PriorityBasedOnCountryPhoneCode`, falling back to `FallBackPriority`), repeated `MaxRoundRobinAttempts` times. Each send works through the queue until a provider succeeds; verification is routed to the provider that sent the OTP. A verification session expires after `SessionTimeoutInSeconds` or `MaximumFailedAttemptsToVerify` failed attempts. Repeated sends inside the resend cooldown return the previous result instead of sending again.

Provider priority is an ordered list, and repeats are meaningful: `[ "Twilio", "Plivo", "Twilio" ]` is a valid priority that tries Twilio, then Plivo, then Twilio again.

Sessions are kept for `Controls:SessionRetentionDays` after they expire — **30 days** by default — and are then removed automatically by a MongoDB TTL index, so the audit trail stays available for recent activity without the collections growing without bound. The same setting governs the `PlivoSession` records. SMSwitch creates the indexes it needs at startup.

Sessions hold the phone number, so set this to whatever your data-retention policy allows rather than leaving it to grow. `0` or less keeps everything indefinitely and removes the expiry index if one is already there. Note a TTL index gives time-based expiry, not erasure of one person's data on request.

## Getting started

### 1. Install

```bash
dotnet add package SMSwitch
```

### 2. Prerequisites

| Requirement | Version | Why |
| --- | --- | --- |
| .NET | 10.0 | The package targets `net10.0` and references the ASP.NET Core shared framework, so it needs an ASP.NET Core host. |
| MongoDB | 4.2 or newer | Session cleanup uses a TTL index, and country feedback uses an aggregation-pipeline update, which 4.2 introduced. |

SMSwitch builds on two companion packages that are installed automatically but need configuration:

- [MongoDbService](https://www.nuget.org/packages/MongoDbService) — provides the MongoDB connection. Requires a `MongoDbSettings` section (connection string + database name).
- [uSignIn.CommonSettings](https://www.nuget.org/packages/uSignIn.CommonSettings) — provides your application's public base URL, which SMSwitch uses to build the Plivo delivery-notification callback URL. Requires a `Settings` section with a `BaseUrl`.

### 3. Configure

Add the following to your `appsettings.json` and adjust the values (keep real credentials in user secrets or environment variables):

```json
{
  "Settings": {
    "BaseUrl": "https://your-public-hostname/"
  },
  "MongoDbSettings": {
    "ConnectionString": "MovedToSecret",
    "DatabaseName": "MyDatabase"
  },
  "SMSwitchSettings": {
    "SupportedCountriesIsoCodes": [ "IN", "FI", "DK" ],
    "Controls": {
      "MaximumFailedAttemptsToVerify": 4,
      "SessionTimeoutInSeconds": 240,
      "MaxRoundRobinAttempts": 2,
      "PriorityBasedOnCountryPhoneCode": {
        "44": [ "Twilio", "Plivo" ],
        "45": [ "Twilio", "Plivo" ],
        "91": [ "Plivo", "Twilio" ]
      },
      "FallBackPriority": [ "Twilio", "Plivo" ]
    },
    "AndroidAppHash": "MovedToSecret",
    "OtpLength": 6,
    "Twilio": {
      "AccountSid": "MovedToSecret",
      "AuthToken": "MovedToSecret",
      "ServiceSid": "MovedToSecret",
      "RegisteredSenderPhoneNumber": "MovedToSecret"
    },
    "Plivo": {
      "AuthId": "MovedToSecret",
      "AuthToken": "MovedToSecret",
      "AppUuid": "MovedToSecret",
      "SourceNumber": "MovedToSecret"
    }
  }
}
```

| Setting | Required | Meaning |
| --- | --- | --- |
| `SupportedCountriesIsoCodes` | **Yes** | Countries marked as supported in the country database. An empty array means all countries are supported, but the key itself must be present. |
| `Controls:PriorityBasedOnCountryPhoneCode` | **Yes** | Provider order per country phone code. May be an empty object, but the key must be present. |
| `Controls:FallBackPriority` | **Yes** | Provider order for phone codes not listed above. Must name at least one known provider. |
| `Controls:MaximumFailedAttemptsToVerify` | No | Failed verification attempts before a session expires (default 3). |
| `Controls:SessionTimeoutInSeconds` | No | Lifetime of an OTP session (default 240). |
| `Controls:MaxRoundRobinAttempts` | No | How many times the provider priority list is repeated in the retry queue (default 1). |
| `Controls:SessionRetentionDays` | No | Days a session is kept after it expires, before a TTL index removes it (default 30). `0` or less keeps them indefinitely. Also applies to `PlivoSession`. |
| `AndroidAppHash` | No | Your Android app hash for SMS Retriever auto-read. |
| `OtpLength` | No | OTP digit count (default 6). See the note below — this writes to your Twilio account. |
| `Twilio:RegisteredSenderPhoneNumber` | For plain SMS | Sender number for plain SMS via Twilio. Not needed for OTPs, which go through Twilio Verify. |
| `Plivo:SourceNumber` | For plain SMS | Sender number for plain SMS via Plivo. Not needed for OTPs. |

`Twilio:AccountSid`, `Twilio:AuthToken` and `Twilio:ServiceSid` are required to enable Twilio at
all; if any is missing the provider is disabled with a logged warning rather than failing per send.
The same goes for `Plivo:AuthId`, `Plivo:AuthToken` and `Plivo:AppUuid`.

> **`OtpLength` writes to your Twilio account.** On startup SMSwitch calls the Twilio Verify API to
> set the code length on the Verify **Service** identified by `ServiceSid`. That is account-side
> configuration shared by everything using that service, not a per-request option. Plivo's length is
> fixed at 6 and cannot be changed from here, so a warning is logged if the two disagree.

#### Configuration errors fail at startup, not at send time

The three required keys above are read eagerly, so a missing one throws while the host is being
built rather than on the first send. Two cases are worth knowing about because the message is not
especially friendly:

- Omitting `SupportedCountriesIsoCodes` or `Controls:PriorityBasedOnCountryPhoneCode` throws
  `InvalidOperationException` naming the missing section.
- An unrecognised country code in `SupportedCountriesIsoCodes` throws `ArgumentException` from the
  enum parse. Codes are ISO 3166-1 alpha-2, for example `DK`, not `DNK` or `Denmark`.

An unrecognised **provider** name is treated more leniently: the offending entry is dropped with a
logged warning and that country falls back to `FallBackPriority`, rather than bringing the
application down. `FallBackPriority` itself is the exception — if nothing in it parses, that does
throw, since there would be no provider left to send with.

#### Webhook authentication

The delivery-notification webhook authenticates callers using Plivo's own request signature
(`X-Plivo-Signature-V3`), checked against your `Plivo:AuthToken`. There is nothing extra to
configure, and no secret travels in the callback URL. The webhook fails closed: if Plivo is not
configured, or a call arrives with a missing or invalid signature, it is rejected with
`401 Unauthorized`.

### 4. Register the services

```csharp
using MongoDbService;
using SMSwitch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMongoDbServices();
builder.Services.AddSMSwitchServices();

var app = builder.Build();

// Maps the Plivo delivery-notification webhook at /smswitch/plivonotification.
// Required if you use Plivo; harmless otherwise.
app.AddSMSwitchApiEndpoints();

app.Run();
```

### 5. Use it

Dependency-inject `SMSwitchService` wherever you need it:

```csharp
using HumanLanguages;
using SMSwitch;
using SMSwitch.Common.DTOs;

public sealed class SignInFlow
{
	private readonly SMSwitchService _smSwitch;

	public SignInFlow(SMSwitchService smSwitch) => _smSwitch = smSwitch;

	public async Task<bool> Demo()
	{
		var mobileNumber = new MobileNumber
		{
			CountryIsoCodeString = "DK",
			CountryPhoneCode = "45",
			PhoneNumber = "12345678"
		};
		var preferredLanguages = new HashSet<LanguageIsoCode> { HumanHelper.CreateLanguageIsoCode("en") };

		// Send a one-time password (provider is picked from your configured priorities)
		var sendResponse = await _smSwitch.SendOTP(mobileNumber, preferredLanguages, UserAgent.WebBrowser);
		// sendResponse.IsSent, sendResponse.OtpLength

		// Later, verify the OTP the user typed in. Check Verified first, see below.
		var verifyResponse = await _smSwitch.VerifyOTP(mobileNumber, "123456");

		// Or send a plain SMS
		var smsSent = await _smSwitch.SendSMS(mobileNumber, "Hello from SMSwitch!");

		return verifyResponse.Verified;
	}
}
```

Every method takes an optional `CancellationToken` as its last parameter. Passing one is worth it:
the delivery-confirmation polling below can otherwise keep running after the caller has gone away.

```csharp
var sendResponse = await _smSwitch.SendOTP(
	mobileNumber, preferredLanguages, UserAgent.WebBrowser,
	resendCooldownPeriodInSeconds: 30, deliveryConfirmationTimeoutInSeconds: 5, cancellationToken);
```

#### Phone number formats

`MobileNumber` takes the country phone code and the national number separately, and strips anything
that is not a digit from both. All of these describe the same Danish number:

```csharp
new MobileNumber { CountryIsoCodeString = "DK", CountryPhoneCode = "45",   PhoneNumber = "12345678"    }
new MobileNumber { CountryIsoCodeString = "DK", CountryPhoneCode = "+45",  PhoneNumber = "12 34 56 78" }
new MobileNumber { CountryIsoCodeString = "DK", CountryPhoneCode = "(+45)", PhoneNumber = "12-34-56-78" }
```

Two things to know:

- **Leading zeros are kept.** A number written with a national trunk zero, such as `0612345678`,
  keeps that zero. Strip it yourself if the destination expects the number without it — SMSwitch
  will not guess.
- **Call `IsValid()` before sending** if the number came from user input. It returns false when
  either part contains no digits at all. SMSwitch also checks internally and returns a failed
  response rather than throwing, but checking first lets you show a useful validation message
  instead of a failed send.

`CountryIsoCodeString` is ISO 3166-1 alpha-2 and is case-insensitive; it is used to record the
observed number length against the country, not to route the message.

#### Reading the verify response

**Check `Verified` before `Expired`.** `Expired` means "this session can no longer be used", and a
successful verification consumes the session, so `Expired` is `true` on success as well as on
failure. Branching on `Expired` first will send a user who just entered the right code back to the
start of the flow.

```csharp
if (verifyResponse.Verified)      { /* signed in */ }
else if (verifyResponse.Expired)  { /* out of attempts or timed out: start a new SendOTP */ }
else                              { /* wrong code, let them try again */ }
```

#### Cooldown and delivery timeout

`SendOTP` and `SendSMS` take two separate durations, both defaulting to 60 seconds:

| Parameter | Controls |
| --- | --- |
| `resendCooldownPeriodInSeconds` | How long a repeated send returns the previous result instead of sending again, so a user hammering "resend" is not billed twice. |
| `deliveryConfirmationTimeoutInSeconds` | How long to wait for the provider to confirm delivery. For plain SMS, and for Plivo OTPs, SMSwitch polls every two seconds until the message is confirmed or this elapses. |

Only the second one makes a call block, so it is the one to keep short on an interactive request
path. Pass a `CancellationToken` as well, so a client disconnect ends the wait rather than letting
it run to the timeout.

```csharp
// Don't resend for a minute, but don't hold the request more than 5 seconds waiting for delivery.
await _smSwitch.SendSMS(mobileNumber, "Your order shipped",
	resendCooldownPeriodInSeconds: 60, deliveryConfirmationTimeoutInSeconds: 5, cancellationToken);
```

## Local testing without real SMS

For local development you can route messages to the `DevConsole` provider instead of Twilio or Plivo, so no credits are spent and no credentials are needed. The OTP (or SMS text) is printed to the console via the logger, and OTPs are generated and verified through [MongoDbTokenManager](https://www.nuget.org/packages/MongoDbTokenManager) in your own MongoDB instance — the full `SendOTP` → `VerifyOTP` flow works end to end.

Put this in your `appsettings.Development.json`:

```json
{
  "SMSwitchSettings": {
    "Controls": {
      "PriorityBasedOnCountryPhoneCode": {
        "45": [ "DevConsole" ]
      },
      "FallBackPriority": [ "DevConsole" ]
    }
  }
}
```

As a safety measure the `DevConsole` provider refuses to operate when the app runs in the `Production` environment: it logs a critical error and reports the send as failed, so the provider queue falls through to a real provider if one is configured.

> Outside `Production`, Plivo OTP sends skip delivery confirmation and report success as soon as the
> Verify session is created, because the delivery webhook cannot reach a developer machine. Only
> `Production` waits for the real notification, so a send that succeeds locally is not by itself
> evidence that the message was delivered.

## Upgrading

This release contains breaking changes. Recompiling is required — the changed signatures are
source-compatible for most callers, but not binary-compatible.

**Configuration**

- `SMSwitchSettings:Plivo:WebhookSecret` has been **removed**. Delete it. The delivery webhook now
  authenticates callers with Plivo's own request signature checked against `Plivo:AuthToken`, so no
  secret travels in the callback URL. Nothing needs re-registering with Plivo, because the callback
  URL is supplied per verification session rather than configured once.

**API**

- Every method on `SMSwitchService` gained an optional trailing `CancellationToken`.
- `resendCooldownPeriodInSeconds` has been split in two. It previously set both the resend window
  and the delivery-confirmation timeout, so shortening it to stop a request blocking also disabled
  resend deduplication. `SendOTP` and `SendSMS` now take `resendCooldownPeriodInSeconds` and
  `deliveryConfirmationTimeoutInSeconds` separately, both still defaulting to 60. Existing positional
  calls such as `SendOTP(number, languages, userAgent, 30)` keep compiling, and 30 still means the
  cooldown — but the delivery timeout now defaults to 60 rather than following it, so check any call
  that relied on a short value to bound how long the request could block. A call that also passed a
  `CancellationToken` positionally will no longer compile, since that argument now lands on the new
  `byte`; pass it by name or add the timeout.
- `IServiceMobileNumbers` is the single-provider contract and its parameter is now
  `deliveryConfirmationTimeoutInSeconds`; it has no resend cooldown, because deduplication is the
  switchboard's job. `SMSwitchService` still implements the interface, explicitly.
- `SmsControls.PriorityBasedOnCountryPhoneCode` and `SmsControls.FallBackPriority` are now
  `List<SmsProvider>` instead of `HashSet<SmsProvider>`. They are ordered priorities, and a set
  guaranteed neither the order nor the ability to repeat a provider.
- `SMSwitchService`'s constructor takes an `IServiceProvider` instead of the three concrete provider
  services. This only affects code constructing it by hand; dependency injection is unchanged.

**Data**

- Sessions are now removed 30 days after they expire, by a TTL index created at startup. On the
  first run this deletes anything already older than that. If you need a longer audit window, change
  it before deploying.
- Phone numbers are no longer parsed through `long`, so a leading trunk zero is preserved rather
  than silently dropped. Numbers written that way now resolve to a different session key, so any
  session in flight for such a number at the moment of deployment will not be found. Those numbers
  were previously being sent to the wrong destination, so this is the correction landing.
- `SendSMS` sessions use a surrogate `_id` with the recipient-and-message hash moved to an indexed
  `DedupeKey` field. Existing documents are simply ignored; nothing needs migrating.

## Contributing

We welcome contributions! If you find a bug or have an idea for an improvement, please submit an issue or a pull request on [GitHub](https://github.com/prmeyn/SMSwitch). The repository includes a `TestAPIs` project — a minimal ASP.NET Core app with Swagger and ready-made `.http` requests for exercising the library locally.

> ⚠️ `TestAPIs` is a local development harness, not a deployable service. Its endpoints have no authentication and send real SMS at your account's expense, so they are only mapped when the app runs in the `Development` environment. Do not remove that guard.

## License

This project is licensed under the [MIT License](LICENSE) — use it in anything, including closed
source and commercial work, as long as the copyright notice travels with it.

Happy coding! 🚀🌐📚
