using SMSwitchCommon;

namespace SMSwitchTelesign
{
	public sealed class TelesignSettings : SMSwitchGeneralSettings
	{
		public required TelesignPrivateSettings TelesignPrivateSettings { get; init; }
	}
	public sealed class TelesignPrivateSettings
	{
		public required string CustomerId { get; init; }
		public required string ApiKey { get; init; }
	}
}
