using MongoDB.Bson.Serialization.Attributes;

namespace SMSwitchPlivo.Database.DTOs
{
	public sealed class PlivoSession
	{
		[BsonId]
		public required string CountryPhoneCodeAndPhoneNumber { get; set; }
		public required string SessionUUID { get; set; }
		public required DateTimeOffset TimeStamp { get; set; }
	}
}
