using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plivo;
using SMSwitch.Common;
using SMSwitch.Services.Plivo.WebHook;
using uSignIn.CommonSettings.Settings;

namespace SMSwitch.Services.Plivo
{
	public sealed class PlivoInitializer : SMSwitchGeneralInitializer
	{
		internal readonly PlivoSettings PlivoSettings;
		internal readonly PlivoApi PlivoApi;
		private readonly SettingsService _settingsService;

		public PlivoInitializer(
			IConfiguration configuration,
			SettingsService settingsService,
			ILogger<PlivoInitializer> logger): base(configuration)
		{
			_settingsService = settingsService;
			try
			{
				var plivoConfig = SMSwitchSettings.GetSection(SmsProvider.Plivo.ToString());


				PlivoSettings = new PlivoSettings()
				{
					AndroidAppHash = SMSwitchGeneralSettings.AndroidAppHash,
					OtpLength = 6, // SMSwitchGeneralSettings.OtpLength , can only be changed via the Plivo backoffice
					PlivoPrivateSettings = new PlivoPrivateSettings()
					{
						AuthId = plivoConfig["AuthId"],
						AuthToken = plivoConfig["AuthToken"],
						AppUuid = plivoConfig["AppUuid"]
					}
				};

				if (SMSwitchGeneralSettings.OtpLength != PlivoSettings.OtpLength)
				{
					logger.LogWarning($"Application is trying to set OTP length to {SMSwitchGeneralSettings.OtpLength} but Plivo is fixed at {PlivoSettings.OtpLength}");
				}

				PlivoApi = new PlivoApi(PlivoSettings.PlivoPrivateSettings.AuthId, PlivoSettings.PlivoPrivateSettings.AuthToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unable to initialize Plivo");
			}
			
		}

		internal string NotificationUrl => new Uri(_settingsService.BaseUri, $"{ConstantStrings.SMSwitchGroupName}{PlivoNotificationEndpoint.PlivoNotificationRoute}").ToString();
	}
}
