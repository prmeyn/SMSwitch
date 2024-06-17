using HumanLanguages;
using Microsoft.Extensions.Configuration;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using SMSwitchTelesign;
using SMSwitchTwilio;

namespace SMSwitch
{
	public sealed class SMSwitchService : IServiceMobileNumbers
	{

		private readonly TwilioService _twilioService;
		private readonly TelesignService _telesignService;
		public SMSwitchService(
			IConfiguration configuration,
			TwilioService twilioService,
			TelesignService telesignService
			)
		{
			_twilioService = twilioService;
			_telesignService = telesignService;
		}

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice)
		{
			return _telesignService.SendOTP(mobileWithCountryCode, languageISOCodeList, isAndroidDevice);
		}

		public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			return _telesignService.SendSMS(mobileWithCountryCode, shortMessageServiceMessage);
		}

		public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			return _telesignService.VerifyOTP(mobileWithCountryCode, OTP);
		}
	}
}
