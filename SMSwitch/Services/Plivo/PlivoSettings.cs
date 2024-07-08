using SMSwitch.Common;

namespace SMSwitch.Services.Plivo
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
