using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Verify.V2;

namespace SMSwitch
{
	public sealed class SMSwitchService
	{
		private readonly SMSwitchSettings _settings;

		public SMSwitchService(IConfiguration configuration)
		{
			var smSwitchSettings = configuration.GetSection("SMSwitchSettings");

			byte defaultLength = 6;
			var otpLength = byte.TryParse(smSwitchSettings["OtpLength"], out byte l) ? l : defaultLength;

			var twilioConfig = smSwitchSettings.GetSection("Twilio");


			_settings = new SMSwitchSettings() 
			{
				AndroidAppHash = smSwitchSettings["AndroidAppHash"],
				OtpLength = otpLength,
				Twilio = new TwilioSettings() 
				{
					AccountSid = twilioConfig["AccountSid"],
					AuthToken = twilioConfig["AuthToken"],
					ServiceSid = twilioConfig["ServiceSid"],
					RegisteredSenderPhoneNumber = twilioConfig["RegisteredSenderPhoneNumber"],
				} 
			};

			TwilioClient.Init(_settings.Twilio.AccountSid, _settings.Twilio.AuthToken);

			_ = ServiceResource.UpdateAsync(
				codeLength: _settings.OtpLength,
				pathSid: _settings.Twilio.ServiceSid
			);
		}

		public string TwilioServiceSid => _settings.Twilio.ServiceSid;
		public string? AndroidAppHash => _settings.AndroidAppHash;
		public byte OtpLength => _settings.OtpLength;
	}
}
