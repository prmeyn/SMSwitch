namespace SMSwitchCommon
{
	public sealed class SmsControls
	{
		public int SessionTimeoutInSeconds { get; set; }
		public byte MaxRoundRobinAttempts { get; set; }
		public Dictionary<byte, HashSet<SmsProvider>> PriorityBasedOnCountryPhoneCode { get; set; }
		public HashSet<SmsProvider> FallBackPriority { get; set; }
	}
}
