namespace SMSwitch.Common.DTOs
{
	public sealed class SMSwitchResponseVerifyOTP
	{
		public bool Verified { get; init; }
		public bool Expired { get; init; } = false;
	}
}
