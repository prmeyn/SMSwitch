using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using SMSwitchPlivo.Database;

namespace SMSwitchPlivo
{
	public sealed class PlivoService : IServiceMobileNumbers
	{
		private readonly PlivoInitializer _plivoInitializer;
		private readonly ILogger<PlivoService> _logger;
		private readonly PlivoDbService _plivoDbService;

		public PlivoService(PlivoInitializer plivoInitializer, ILogger<PlivoService> logger, PlivoDbService plivoDbService)
		{
			_plivoInitializer = plivoInitializer;
			_logger = logger;
			_plivoDbService = plivoDbService;
		}

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, UserAgent userAgent)
		{
			try 
			{
				var verifySessionResponse = _plivoInitializer.PlivoApi.VerifySession.Create(
				recipient: mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber,
				app_uuid: _plivoInitializer.PlivoSettings.PlivoPrivateSettings.AppUuid,
				channel: "sms");

				_plivoDbService.SetLatestSessionUUID(mobileWithCountryCode, verifySessionResponse.SessionUUID).Wait();

				return new SMSwitchResponseSendOTP()
				{
					IsSent = verifySessionResponse.StatusCode.ToString().StartsWith("2"),
					OtpLength = _plivoInitializer.PlivoSettings.OtpLength
				};
			}
			catch
			{
				return new SMSwitchResponseSendOTP()
				{
					IsSent = false
				};
			}
			
		}
		public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			var sessionUuid = _plivoDbService.GetLatestSessionUUID(mobileWithCountryCode).Result;

			try
			{
				var response = _plivoInitializer.PlivoApi.VerifySession.Validate(session_uuid: sessionUuid, otp: OTP);
			} catch
			{
				return false;
			}
			

			if (_plivoInitializer.PlivoApi.VerifySession.Get(sessionUuid).Status.ToLower() == "verified")
			{
				_plivoDbService.ClearSessionUUID(mobileWithCountryCode);
				return true;
			}
			return false;
		}
	}
}
