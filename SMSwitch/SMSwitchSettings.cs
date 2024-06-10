namespace SMSwitch
{
	public sealed class SMSwitchSettings
	{
		public required string AndroidAppHash { get; set; }
		public byte OtpLength { get; set; }
		public TwilioSettings Twilio { get; set; }
	}

	public sealed class TwilioSettings
	{
		public required string AccountSid { get; set; }
		public required string AuthToken { get; set; }
		public required string ServiceSid { get; set; }
		public required string RegisteredSenderPhoneNumber { get; set; }
	}
}
