namespace SMSwitch.Common
{
	public sealed class SmsControls
	{
		public byte MaximumFailedAttemptsToVerify { get; init; }
		public int SessionTimeoutInSeconds { get; init; }
		public byte MaxRoundRobinAttempts { get; set; }
		public Dictionary<string, HashSet<SmsProvider>> PriorityBasedOnCountryPhoneCode { get; set; }
		public HashSet<SmsProvider> FallBackPriority { get; set; }
	}
}
