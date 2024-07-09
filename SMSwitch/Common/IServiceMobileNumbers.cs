using HumanLanguages;
using SMSwitch.Common.DTOs;

namespace SMSwitch.Common
{
	public interface IServiceMobileNumbers
	{
		Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent);
		Task<SMSwitchResponseVerifyOTP> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP);
		Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage);
	}
}
