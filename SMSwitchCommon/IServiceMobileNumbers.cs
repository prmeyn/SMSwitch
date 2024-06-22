using HumanLanguages;
using SMSwitchCommon.DTOs;

namespace SMSwitchCommon
{
	public interface IServiceMobileNumbers
	{
		Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent);
		Task<bool> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP);
		Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage);
	}
}
