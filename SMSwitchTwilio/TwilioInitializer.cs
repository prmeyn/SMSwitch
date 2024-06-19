using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMSwitchCommon;
using Twilio;
using Twilio.Rest.Verify.V2;

namespace SMSwitchTwilio
{
	public sealed class TwilioInitializer: SMSwitchGeneralInitializer
	{
		internal readonly TwilioSettings TwilioSettings;
		public TwilioInitializer(
			IConfiguration configuration,
			ILogger<TwilioInitializer> logger) : base(configuration)
		{
			try {
				
				var twilioConfig = SMSwitchSettings.GetSection("Twilio");

				TwilioSettings = new TwilioSettings()
				{
					AndroidAppHash = SMSwitchGeneralSettings.AndroidAppHash,
					OtpLength = SMSwitchGeneralSettings.OtpLength,
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
