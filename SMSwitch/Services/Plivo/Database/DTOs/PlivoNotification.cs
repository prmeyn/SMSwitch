
namespace SMSwitch.Services.Plivo.Database.DTOs
{
	public sealed record PlivoNotification(byte attemptSequence, string attemptUUID, string channel, string channelErrorCode, string channelStatus, DateTime requestTime, string sessionStatus, DateTimeOffset utcNow);
}
