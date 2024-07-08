using SMSwitch.Common;

namespace SMSwitch.Services.Twilio
{
	public sealed class TwilioSettings : SMSwitchGeneralSettings
	{
		public required TwilioPrivateSettings TwilioPrivateSettings { get; init; }
	}
	public sealed class TwilioPrivateSettings
	{
		public required string AccountSid { get; init; }
		public required string AuthToken { get; init; }
		public required string ServiceSid { get; init; }
		public required string RegisteredSenderPhoneNumber { get; init; }
	}
}
