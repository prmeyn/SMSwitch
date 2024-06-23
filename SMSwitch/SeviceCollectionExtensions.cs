using Countries;
using Microsoft.Extensions.DependencyInjection;
using SMSwitch.Database;
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
			services.AddSingleton<CountryInitializer>();
			services.AddSingleton<CountryDbService>();
			services.AddHostedService<CountryDbService>();

			services.AddSingleton<SMSwitchInitializer>();
			services.AddSingleton<SMSwitchDbService>();

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
