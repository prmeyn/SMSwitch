namespace SMSwitchCommon
{
	public sealed class SmsControls
	{
		public int MaxRoundRobinAttempts { get; set; }
		public Dictionary<byte, HashSet<SmsProvider>> PriorityBasedOnCountryPhoneCode { get; set; }
		public HashSet<SmsProvider> FallBackPriority { get; set; }
	}
}
