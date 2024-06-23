﻿using HumanLanguages;
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

		public async Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent)
		{
			try 
			{
				var verifySessionResponse = _plivoInitializer.PlivoApi.VerifySession.Create(
					recipient: mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber,
					app_uuid: _plivoInitializer.PlivoSettings.PlivoPrivateSettings.AppUuid,
					channel: "sms");

				await _plivoDbService.SetLatestSessionUUID(mobileWithCountryCode, verifySessionResponse.SessionUUID);

				return new SMSwitchResponseSendOTP()
				{
					IsSent = verifySessionResponse.StatusCode.ToString().StartsWith("2"),
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

		public async Task<bool> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			try
			{
				var sessionUuid = await _plivoDbService.GetLatestSessionUUID(mobileWithCountryCode);
				var response = _plivoInitializer.PlivoApi.VerifySession.Validate(session_uuid: sessionUuid, otp: OTP);
				if (_plivoInitializer.PlivoApi.VerifySession.Get(sessionUuid).Status.ToLower() == "verified")
				{
					await _plivoDbService.ClearSessionUUID(mobileWithCountryCode);
					return true;
				}
				return false;
			}
			catch(Exception exception)
			{
				_logger.LogError(exception, $"Could not verify OTP for +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
				return false;
			}
		}
	}
}
