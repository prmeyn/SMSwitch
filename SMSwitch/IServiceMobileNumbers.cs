using HumanLanguages;
using SMSwitch.DTOs;

namespace SMSwitch
{
	public interface IServiceMobileNumbers
	{
		bool SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, string? appHash);
		bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP);
		bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage);
	}
}
