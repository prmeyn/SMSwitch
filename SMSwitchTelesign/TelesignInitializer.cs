using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SMSwitchCommon;
using TelesignEnterprise;

namespace SMSwitchTelesign
{
	public class TelesignInitializer: SMSwitchGeneralInitializer
	{
		internal readonly TelesignSettings TelesignSettings;
		internal readonly VerifyClient VerifyClient;

		public TelesignInitializer(
			IConfiguration configuration,
			ILogger<TelesignInitializer> logger): base(configuration)
		{
			try
			{
				
				var telesignConfig = SMSwitchSettings.GetSection("Telesign");

				TelesignSettings = new TelesignSettings()
				{
					AndroidAppHash = SMSwitchGeneralSettings.AndroidAppHash,
					OtpLength = SMSwitchGeneralSettings.OtpLength,
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
