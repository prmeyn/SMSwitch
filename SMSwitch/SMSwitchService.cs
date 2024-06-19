using HumanLanguages;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using SMSwitchPlivo;
using SMSwitchTelesign;
using SMSwitchTwilio;

namespace SMSwitch
{
	public sealed class SMSwitchService : IServiceMobileNumbers
	{

		private readonly SMSwitchInitializer _smSwitchInitializer;

		private readonly TwilioService _twilioService;
		private readonly TelesignService _telesignService;
		private readonly PlivoService _plivoService;
		

		public SMSwitchService(
			SMSwitchInitializer smSwitchInitializer,
			TwilioService twilioService,
			TelesignService telesignService,
			PlivoService plivoService
			)
		{
			_smSwitchInitializer = smSwitchInitializer;
			_twilioService = twilioService;
			_telesignService = telesignService;
			_plivoService = plivoService;
		}

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice)
		{
			var x = _smSwitchInitializer.SmsControls.FallBackPriority;

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
