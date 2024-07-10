using MongoDB.Bson.Serialization.Attributes;

namespace SMSwitch.Services.Plivo.Database.DTOs
{
	public sealed class PlivoSession
	{
		[BsonId]
		public required string CountryPhoneCodeAndPhoneNumber { get; set; }
		public required string SessionUUID { get; set; }
		public required DateTimeOffset TimeStamp { get; set; }
		public List<PlivoNotification> Notifications { get; init; } = [];
	}
}
