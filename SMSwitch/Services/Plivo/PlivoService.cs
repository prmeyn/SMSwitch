using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitch.Common;
using SMSwitch.Common.DTOs;
using SMSwitch.Services.Plivo.Database;

namespace SMSwitch.Services.Plivo
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

		public async Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent)
		{
			try 
			{
				var verifySessionResponse = _plivoInitializer.PlivoApi.VerifySession.Create(
					recipient: mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber,
					app_uuid: _plivoInitializer.PlivoSettings.PlivoPrivateSettings.AppUuid,
					url: _plivoInitializer.NotificationUrl ,
					method: "GET",
					channel: "sms",
					locale: preferredLanguageIsoCodeList.First().ToIsoCodeString('_'));

				await _plivoDbService.SetLatestSessionUUID(mobileWithCountryCode, verifySessionResponse.SessionUUID);

				bool isSent = false;
				if (verifySessionResponse.StatusCode.ToString().StartsWith("2"))
				{
					isSent = await _plivoDbService.KeepCheckingTheDatabaseIfSentEvery2seconds(verifySessionResponse.SessionUUID, expiry: DateTimeOffset.UtcNow.AddSeconds(60));
				}
				return new SMSwitchResponseSendOTP()
				{
					IsSent = isSent,
					OtpLength = _plivoInitializer.PlivoSettings.OtpLength
				};
			}
			catch(Exception exception)
			{
				_logger.LogError(exception, $"Could not send OTP to +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
				return new SMSwitchResponseSendOTP()
				{
					IsSent = false
				};
			}
		}

		public Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public async Task<SMSwitchResponseVerifyOTP> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			try
			{
				var sessionUuid = await _plivoDbService.GetLatestSessionUUID(mobileWithCountryCode);
				var response = _plivoInitializer.PlivoApi.VerifySession.Validate(session_uuid: sessionUuid, otp: OTP);
				if (_plivoInitializer.PlivoApi.VerifySession.Get(sessionUuid).Status.ToLower() == "verified")
				{
					await _plivoDbService.ClearSessionUUID(mobileWithCountryCode);
					return new SMSwitchResponseVerifyOTP()
					{
						Verified = true
					};
				}
			}
			catch(Exception exception)
			{
				_logger.LogError(exception, $"Could not verify OTP for +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
			}
			return new SMSwitchResponseVerifyOTP(){
				Verified = false
			};
		}
	}
}
