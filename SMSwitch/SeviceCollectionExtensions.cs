using Microsoft.Extensions.DependencyInjection;
using SMSwitch.Common;
using SMSwitch.Countries;
using SMSwitch.Countries.Database;
using SMSwitch.Database;
using SMSwitch.Services.Plivo;
using SMSwitch.Services.Plivo.Database;
using SMSwitch.Services.Twilio;

namespace SMSwitch
{
	public static class SeviceCollectionExtensions
	{
		public static void AddSMSwitchServices(this IServiceCollection services)
		{
			services.AddHttpContextAccessor();

			services.AddSingleton<CountryInitializer>();
			services.AddSingleton<CountryDbService>();
			services.AddHostedService<CountryDbService>();

			services.AddSingleton<SMSwitchInitializer>();
			services.AddSingleton<SMSwitchDbService>();

			services.AddSingleton<TwilioInitializer>();
			services.AddScoped<TwilioService>();

			services.AddSingleton<PlivoInitializer>();
			services.AddSingleton<PlivoDbService>();
			services.AddScoped<PlivoService>();

			services.AddScoped<SMSwitchService>();
		}
	}
}
