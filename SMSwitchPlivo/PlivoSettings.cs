using SMSwitchCommon;

namespace SMSwitchPlivo
{
	public sealed class PlivoSettings : SMSwitchGeneralSettings
	{
		public required PlivoPrivateSettings PlivoPrivateSettings { get; init; }
	}
	public sealed class PlivoPrivateSettings
	{
		public required string AuthId { get; init; }
		public required string AuthToken { get; init; }
		public required string AppUuid { get; init; }
		
	}
}
