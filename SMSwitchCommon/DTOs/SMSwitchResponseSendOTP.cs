namespace SMSwitchCommon.DTOs
{
	public sealed class SMSwitchResponseSendOTP
	{
		public bool IsSent { get; set; }
		public byte OtpLength { get; set; }
	}
}
