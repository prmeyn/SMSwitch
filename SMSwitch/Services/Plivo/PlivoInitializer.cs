using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plivo;
using SMSwitch.Common;

namespace SMSwitch.Services.Plivo
{
	public sealed class PlivoInitializer : SMSwitchGeneralInitializer
	{
		internal readonly PlivoSettings PlivoSettings;
		internal readonly PlivoApi PlivoApi;

		public PlivoInitializer(
			IConfiguration configuration,
			ILogger<PlivoInitializer> logger): base(configuration)
		{
			try
			{
				var plivoConfig = SMSwitchSettings.GetSection(SmsProvider.Plivo.ToString());


				PlivoSettings = new PlivoSettings()
				{
					AndroidAppHash = SMSwitchGeneralSettings.AndroidAppHash,
					OtpLength = SMSwitchGeneralSettings.OtpLength,
					PlivoPrivateSettings = new PlivoPrivateSettings()
					{
						AuthId = plivoConfig["AuthId"],
						AuthToken = plivoConfig["AuthToken"],
						AppUuid = plivoConfig["AppUuid"]
					}
				};

				PlivoApi = new PlivoApi(PlivoSettings.PlivoPrivateSettings.AuthId, PlivoSettings.PlivoPrivateSettings.AuthToken);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unable to initialize Plivo");
			}
			
		}
	}
}
