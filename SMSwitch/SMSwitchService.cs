using HumanLanguages;
using Microsoft.Extensions.Configuration;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using SMSwitchPlivo;
using SMSwitchTelesign;
using SMSwitchTwilio;

namespace SMSwitch
{
	public sealed class SMSwitchService : IServiceMobileNumbers
	{

		private readonly TwilioService _twilioService;
		private readonly TelesignService _telesignService;
		private readonly PlivoService _plivoService;

		public SMSwitchService(
			IConfiguration configuration,
			TwilioService twilioService,
			TelesignService telesignService,
			PlivoService plivoService
			)
		{
			_twilioService = twilioService;
			_telesignService = telesignService;
			_plivoService = plivoService;
		}

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice)
		{
			return _plivoService.SendOTP(mobileWithCountryCode, languageISOCodeList, isAndroidDevice);
		}

		public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			return _plivoService.SendSMS(mobileWithCountryCode, shortMessageServiceMessage);
		}

		public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			return _plivoService.VerifyOTP(mobileWithCountryCode, OTP);
		}
	}
}
