using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Verify.V2;

namespace SMSwitchTwilio
{
	public sealed class TwilioInitializer
	{
		internal readonly TwilioSettings TwilioSettings;
		public TwilioInitializer(
			IConfiguration configuration,
			ILogger<TwilioInitializer> logger)
		{
			try {
				var smSwitchSettings = configuration.GetSection("SMSwitchSettings");

				byte defaultLength = 6;
				var otpLength = byte.TryParse(smSwitchSettings["OtpLength"], out byte l) ? l : defaultLength;

				var twilioConfig = smSwitchSettings.GetSection("Twilio");


				TwilioSettings = new TwilioSettings()
				{
					AndroidAppHash = smSwitchSettings["AndroidAppHash"],
					OtpLength = otpLength,
					TwilioPrivateSettings = new TwilioPrivateSettings()
					{
						AccountSid = twilioConfig["AccountSid"],
						AuthToken = twilioConfig["AuthToken"],
						ServiceSid = twilioConfig["ServiceSid"],
						RegisteredSenderPhoneNumber = twilioConfig["RegisteredSenderPhoneNumber"],
					}
				};

				TwilioClient.Init(TwilioSettings.TwilioPrivateSettings.AccountSid, TwilioSettings.TwilioPrivateSettings.AuthToken);

				_ = ServiceResource.UpdateAsync(
					codeLength: TwilioSettings.OtpLength,
					pathSid: TwilioSettings.TwilioPrivateSettings.ServiceSid
				);
			} catch (Exception ex)
			{
				logger.LogError(ex, "Unable to initialize Twilio");
			}
		}

	}
}
