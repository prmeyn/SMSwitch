using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TelesignEnterprise;

namespace SMSwitchTelesign
{
	public class TelesignInitializer
	{
		internal readonly TelesignSettings TelesignSettings;
		internal readonly VerifyClient VerifyClient;

		public TelesignInitializer(
			IConfiguration configuration,
			ILogger<TelesignInitializer> logger)
		{
			try
			{
				var smSwitchSettings = configuration.GetSection("SMSwitchSettings");

				byte defaultLength = 6;
				var otpLength = byte.TryParse(smSwitchSettings["OtpLength"], out byte l) ? l : defaultLength;

				var telesignConfig = smSwitchSettings.GetSection("Telesign");


				TelesignSettings = new TelesignSettings()
				{
					AndroidAppHash = smSwitchSettings["AndroidAppHash"],
					OtpLength = otpLength,
					TelesignPrivateSettings = new TelesignPrivateSettings()
					{
						CustomerId = telesignConfig["CustomerId"],
						ApiKey = telesignConfig["ApiKey"]
					}
				};

				VerifyClient = new VerifyClient(TelesignSettings.TelesignPrivateSettings.CustomerId, TelesignSettings.TelesignPrivateSettings.ApiKey);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unable to initialize Telesign");
			}
		}
	}
}
