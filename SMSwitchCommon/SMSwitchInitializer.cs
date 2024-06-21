using Microsoft.Extensions.Configuration;

namespace SMSwitchCommon
{
	public sealed class SMSwitchInitializer
	{
		public readonly SmsControls SmsControls;
		public SMSwitchInitializer(IConfiguration configuration)
		{
			var smsControlsConfig = configuration.GetSection("SMSwitchSettings:Controls");
			SmsControls = new SmsControls() {
				SessionTimeoutInSeconds = int.TryParse(smsControlsConfig["SessionTimeoutInSeconds"], out int sessionTimeoutInSeconds) ? sessionTimeoutInSeconds : 240,
				MaxRoundRobinAttempts = byte.TryParse(smsControlsConfig["MaxRoundRobinAttempts"], out byte maxRoundRobinAttempts) ? maxRoundRobinAttempts : (byte)1,
				PriorityBasedOnCountryPhoneCode = smsControlsConfig.GetRequiredSection("PriorityBasedOnCountryPhoneCode")
				.GetChildren()
				.Where(c => byte.TryParse(c.Key, out byte _) && c.Get<string[]>().All(p => Enum.TryParse(p, out SmsProvider _)))
				.ToDictionary(countryCodeSection => byte.Parse(countryCodeSection.Key),
					countryCodeSection => countryCodeSection.Get<string[]>().Select(p => Enum.Parse<SmsProvider>(p)).ToHashSet()),
				FallBackPriority = getFallBackPriority(smsControlsConfig.GetRequiredSection("FallBackPriority").Get<string[]>())
			};
		}

		private HashSet<SmsProvider> getFallBackPriority(string[] value)
		{
			var valuesFromConfig = value.Where(p => Enum.TryParse(p, out SmsProvider _)).Select(p => Enum.Parse<SmsProvider>(p)).ToHashSet();
			if (valuesFromConfig.Count() < 1)
			{
				throw new Exception("FallBackPriority list missing!!");
			}
			return valuesFromConfig;
		}
	}
}
