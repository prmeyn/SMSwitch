# Contributing to SMSwitch

Thanks for your interest in improving SMSwitch! Bug reports, fixes, documentation improvements and ideas
are all welcome.

By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Reporting bugs and requesting features

- **Bugs**: open an issue using the *Bug report* template. A minimal reproduction is the most useful
  thing you can include.
- **Features**: open an issue using the *Feature request* template, ideally before writing code,
  so the approach can be agreed first.
- **Security vulnerabilities**: do **not** open an issue. Follow [SECURITY.md](SECURITY.md) instead.

## Development setup

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

## Building and testing

```bash
dotnet build SMSwitch.sln --configuration Release -warnaserror
dotnet test SMSwitch.sln --configuration Release --no-build
```

The build is kept at **0 warnings**, and CI treats warnings as errors, so please fix any warning your
change introduces rather than suppressing it.

## Pull requests

1. Fork the repository and create a branch from `main`.
2. Keep each pull request focused on one change; unrelated fixes are easier to review separately.
3. Add or update tests for any behaviour you change.
4. Update the README if you change public behaviour or configuration.
5. Make sure the build and tests pass locally, then open the pull request and fill in the template.

Every pull request is built and tested by CI (the `build` check), and must pass before it is merged.

## Dependencies

Dependabot proposes dependency updates weekly, grouped into a few pull requests. Major-version
upgrades are not proposed automatically and are made deliberately, so please discuss one in an issue
before sending a pull request for it.

## Releases

Releases are published to [nuget.org](https://www.nuget.org/packages/SMSwitch) by the maintainer, by
pushing a version tag. You don't need to change version numbers in your pull request.

## License

By contributing, you agree that your contributions will be licensed under the project's
[MIT License](LICENSE).
