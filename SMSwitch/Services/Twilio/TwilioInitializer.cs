﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMSwitch.Common;
using Twilio;
using Twilio.Rest.Verify.V2;

namespace SMSwitch.Services.Twilio
{
	public sealed class TwilioInitializer: SMSwitchGeneralInitializer
	{
		internal readonly TwilioSettings TwilioSettings;
		public TwilioInitializer(
			IConfiguration configuration,
			ILogger<TwilioInitializer> logger) : base(configuration)
		{
			try {
				
				var twilioConfig = SMSwitchSettings.GetSection(SmsProvider.Twilio.ToString());

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
