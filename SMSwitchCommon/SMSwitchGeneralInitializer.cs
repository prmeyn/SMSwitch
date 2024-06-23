using Microsoft.Extensions.Configuration;

namespace SMSwitchCommon
{
	public class SMSwitchGeneralInitializer
	{
		public readonly SMSwitchGeneralSettings SMSwitchGeneralSettings;
		public readonly IConfigurationSection SMSwitchSettings;
		public SMSwitchGeneralInitializer(IConfiguration configuration)
		{
			SMSwitchSettings = configuration.GetSection(ConstantStrings.SMSwitchSettingsName);

			byte defaultLength = 6;
			var otpLength = byte.TryParse(SMSwitchSettings["OtpLength"], out byte l) ? l : defaultLength;
			var androidAppHash = SMSwitchSettings["AndroidAppHash"];

			SMSwitchGeneralSettings = new SMSwitchGeneralSettings() 
			{
				AndroidAppHash = androidAppHash,
				OtpLength = otpLength
			};
		}
	}
}
