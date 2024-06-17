using HumanLanguages;
using SMSwitchCommon.DTOs;

namespace SMSwitchCommon
{
	public interface IServiceMobileNumbers
	{
		SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice);
		bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP);
		bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage);
	}
}
