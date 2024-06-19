using Microsoft.Extensions.DependencyInjection;
using SMSwitchCommon;
using SMSwitchPlivo;
using SMSwitchPlivo.Database;
using SMSwitchTelesign;
using SMSwitchTwilio;

namespace SMSwitch
{
	public static class SeviceCollectionExtensions
	{
		public static void AddSMSwitchServices(this IServiceCollection services)
		{
			services.AddSingleton<SMSwitchInitializer>();

			services.AddSingleton<TwilioInitializer>();
			services.AddScoped<TwilioService>();

			services.AddSingleton<TelesignInitializer>();
			services.AddScoped<TelesignService>();

			services.AddSingleton<PlivoInitializer>();
			services.AddSingleton<PlivoDbService>();
			services.AddScoped<PlivoService>();

			services.AddScoped<SMSwitchService>();
		}
	}
}
