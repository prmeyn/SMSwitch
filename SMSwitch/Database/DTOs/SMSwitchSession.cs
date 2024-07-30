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
		public List<AttemptDetailsSendOTP> SentAttempts { get; set; } = [];
		public List<DateTimeOffset> FailedVerificationAttemptsDateTimeOffset { get; set; } = [];
		internal bool HasNotExpired(byte maximumFailedAttemptsToVerify) =>
			(SmsProvidersQueue?.Any() ?? true) && // if it has become empty from failed attempts then it has expired
			FailedVerificationAttemptsDateTimeOffset.Count() < maximumFailedAttemptsToVerify &&
			SuccessfullyVerifiedTimestampUTC == null &&
			DateTimeOffset.UtcNow < ExpiryTimeUTC;
	}
}
