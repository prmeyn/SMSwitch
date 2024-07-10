using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SMSwitch.Services.Plivo.WebHook;

namespace SMSwitch
{
	public static class WebApplicationExtensions
	{
		public static WebApplication AddSMSwitchApiEndpoints(this WebApplication app)
		{

			app.MapGroup("smswitch")
				.GroupPlivoNotificationApisV1()
				.WithTags("smswitch");

			return app;
		}
	}
}
