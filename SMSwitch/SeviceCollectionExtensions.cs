using Microsoft.Extensions.DependencyInjection;
using SMSwitch.Twilio;

namespace SMSwitch
{
	public static class SeviceCollectionExtensions
	{
		public static void AddSMSwitchServices(this IServiceCollection services)
		{
			services.AddSingleton<SMSwitchService>();
			services.AddScoped<TwilioService>();
		}
	}
}
