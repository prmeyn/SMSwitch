using MongoDB.Bson.Serialization.Attributes;
using SMSwitch.Common;

namespace SMSwitch.Database.DTOs
{
	public sealed class SMSwitchSession
	{
		[BsonId]
		public required string SessionId { get; init; }
		public required string CountryPhoneCodeAndPhoneNumber { get; init; }
		public required DateTimeOffset StartTimeUTC { get; init; }
		public DateTimeOffset? SuccessfullyVerifiedTimestampUTC { get; set; }
		public required DateTimeOffset ExpiryTimeUTC { get; init; }
		public Queue<SmsProvider>? SmsProvidersQueue { get; set; }
		public List<DateTimeOffset> FailedAtteptsDateTimeOffset { get; set; } = [];
		internal bool HasNotExpired(byte maximumFailedAttempts) => 
			FailedAtteptsDateTimeOffset.Count() < maximumFailedAttempts &&
			SuccessfullyVerifiedTimestampUTC == null &&
			DateTimeOffset.UtcNow < ExpiryTimeUTC;
	}
}
