using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SMSwitch.Services.Plivo.Database;
using SMSwitch.Services.Plivo.Database.DTOs;

namespace SMSwitch.Services.Plivo.WebHook
{
	public static class PlivoNotificationEndpoint
	{
		public const string PlivoNotificationRouteGroup = "smswitch";
		public const string PlivoNotificationRoute = "/plivonotification";
		public static RouteGroupBuilder GroupPlivoNotificationApisV1(this RouteGroupBuilder group)
		{
			group.MapGet(PlivoNotificationRoute, async (
				HttpRequest httpRequest,
				PlivoDbService plivoDbService,
				byte AttemptSequence,
				string AttemptUUID,
				string Channel,
				string ChannelErrorCode,
				string ChannelStatus,
				string Recipient,
				DateTime RequestTime,
				string SessionStatus,
				string SessionUUID) =>
			{
				await  plivoDbService.UpdateSessionUUID(Recipient, SessionUUID, new PlivoNotification(AttemptSequence, AttemptUUID, Channel, ChannelErrorCode, ChannelStatus, RequestTime, SessionStatus, DateTimeOffset.UtcNow));
				return Results.Ok();
			})
			.Produces(StatusCodes.Status200OK);

			return group;
		}

	}
}
