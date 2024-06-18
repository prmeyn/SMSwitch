using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Plivo;

namespace SMSwitchPlivo
{
	public sealed class PlivoInitializer
	{
		internal readonly PlivoSettings PlivoSettings;
		internal readonly PlivoApi PlivoApi;

		public PlivoInitializer(
			IConfiguration configuration,
			ILogger<PlivoInitializer> logger)
		{
			try
			{
				var smSwitchSettings = configuration.GetSection("SMSwitchSettings");

				byte defaultLength = 6;
				var otpLength = byte.TryParse(smSwitchSettings["OtpLength"], out byte l) ? l : defaultLength;

				var plivoConfig = smSwitchSettings.GetSection("Plivo");


				PlivoSettings = new PlivoSettings()
				{
					AndroidAppHash = smSwitchSettings["AndroidAppHash"],
					OtpLength = otpLength,
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
