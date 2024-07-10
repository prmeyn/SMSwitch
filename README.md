# SMSwitch(https://www.nuget.org/packages/SMSwitch)

**SMSwitch** is an open-source C# class library that provides a wrapper around existing services that are used to verify Mobile numbers and send messages. The service stores information in a MongoDb database that you configure using the package [MongoDbService](https://www.nuget.org/packages/MongoDbService) 

## Features

- Covers Twilio, Plivo (possible to cover more if needed)
- Usage information is stored in your own MongoDB instance for audit reasons


## Contributing

We welcome contributions! If you find a bug, have an idea for improvement, please submit an issue or a pull request on GitHub.

## Getting Started

### [NuGet Package](https://www.nuget.org/packages/SMSwitch)

To include **SMSwitch** in your project, [install the NuGet package](https://www.nuget.org/packages/SMSwitch):

```bash
dotnet add package SMSwitch
```
Then in your `appsettings.json` add the following sample configuration and change the values to match the details of your credentials to the various services.
```json
  "SMSwitchSettings": {
    "SupportedCountriesIsoCodes": [ "IN", "FI", "DK" ],
    "Controls": {
      "MaximumFailedAttempts": 4,
      "SessionTimeoutInSeconds": 240,
      "MaxRoundRobinAttempts": 2,
      "PriorityBasedOnCountryPhoneCode": {
        "44": [ "Twilio", "Plivo" ],
        "45": [ "Twilio", "Plivo" ],
        "91": [ "Plivo", "Twilio"]
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
      "WebHookBaseUri": "https://your-server-base-url",
      "AuthId": "MovedToSecret",
      "AuthToken": "MovedToSecret",
      "AppUuid": "MovedToSecret"
    }
  }
  ```

After the above is done, you can just Dependency inject the `SMSwitch` in your C# class.

#### For example:



```csharp
TODO

```

### GitHub Repository
Visit our GitHub repository for the latest updates, documentation, and community contributions.
https://github.com/prmeyn/SMSwitch


## License

This project is licensed under the GNU GENERAL PUBLIC LICENSE.

Happy coding! 🚀🌐📚



