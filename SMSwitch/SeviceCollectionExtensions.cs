using Microsoft.Extensions.DependencyInjection;
using SMSwitchTelesign;
using SMSwitchTwilio;

namespace SMSwitch
{
	public static class SeviceCollectionExtensions
	{
		public static void AddSMSwitchServices(this IServiceCollection services)
		{
			services.AddSingleton<TwilioInitializer>();
			services.AddScoped<TwilioService>();

			services.AddSingleton<TelesignInitializer>();
			services.AddScoped<TelesignService>();

			services.AddScoped<SMSwitchService>();
		}
	}
}
