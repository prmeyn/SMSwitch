using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SMSwitch.Common;
using SMSwitch.Services.Plivo.WebHook;

namespace SMSwitch
{
	public static class WebApplicationExtensions
	{
		public static WebApplication AddSMSwitchApiEndpoints(this WebApplication app)
		{

			app.MapGroup(ConstantStrings.SMSwitchGroupName)
				.GroupPlivoNotificationApisV1()
				.WithTags(ConstantStrings.SMSwitchTagName);

			return app;
		}
	}
}
