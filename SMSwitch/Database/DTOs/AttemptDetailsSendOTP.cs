using SMSwitch.Common;

namespace SMSwitch.Database.DTOs
{
	public record AttemptDetailsSendOTP(DateTimeOffset AttemptTimeInUTC, SmsProvider SmsProvider, bool SentSuccessfully);
}
