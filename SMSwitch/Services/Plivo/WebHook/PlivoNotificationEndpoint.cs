using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
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
				[FromServices] HttpRequest httpRequest,
				[FromServices] PlivoDbService plivoDbService,
				[FromServices] ILogger logger,
				[FromQuery] byte AttemptSequence,
				[FromQuery] string AttemptUUID,
				[FromQuery] string Channel,
				[FromQuery] string ChannelErrorCode,
				[FromQuery] string ChannelStatus,
				[FromQuery] string Recipient,
				[FromQuery] DateTime RequestTime,
				[FromQuery] string SessionStatus,
				[FromQuery] string SessionUUID) =>
			{
				try 
				{
					await plivoDbService.UpdateSessionUUID(Recipient, SessionUUID, new PlivoNotification(AttemptSequence, AttemptUUID, Channel, ChannelErrorCode, ChannelStatus, RequestTime, SessionStatus, DateTimeOffset.UtcNow));
					return Results.Ok();
				} 
				catch (Exception ex) 
				{
					logger.LogCritical(ex, $"Recipient: {Recipient}  SessionUUID: {SessionUUID}");
				}
				return Results.Problem();
			})
			.Produces(StatusCodes.Status200OK);

			return group;
		}

	}
}
