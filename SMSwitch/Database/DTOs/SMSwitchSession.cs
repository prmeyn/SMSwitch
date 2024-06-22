using MongoDB.Bson.Serialization.Attributes;
using SMSwitchCommon;

namespace SMSwitch.Database.DTOs
{
	public sealed class SMSwitchSession
	{
		[BsonId]
		public required string SessionId { get; set; }
		public required string CountryPhoneCodeAndPhoneNumber { get; set; }
		public required DateTimeOffset StartTimeUTC { get; set; }
		public required DateTimeOffset ExpiryTimeUTC { get; set; }
		public Queue<SmsProvider> SmsProvidersQueue { get; set; }

		internal bool HasNotExpired()
		{
			return DateTimeOffset.UtcNow < ExpiryTimeUTC;
		}
	}
}
