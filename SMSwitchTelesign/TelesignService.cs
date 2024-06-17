using HumanLanguages;
using Microsoft.Extensions.Logging;
using MongoDbTokenManager;
using MongoDbTokenManager.Database;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using Telesign;

namespace SMSwitchTelesign
{
	public sealed class TelesignService : IServiceMobileNumbers
	{
		private readonly TelesignInitializer _telesignInitializer;
		private readonly ILogger<TelesignService> _logger;
		private readonly MongoDbTokenService _mongoDbTokenService;
		private readonly string _logId = "TelesignService";

		public TelesignService(TelesignInitializer telesignInitializer, ILogger<TelesignService> logger, MongoDbTokenService mongoDbTokenService)
		{
			_telesignInitializer = telesignInitializer;
			_logger = logger;
			_mongoDbTokenService = mongoDbTokenService;
		}

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice)
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>() 
			{
				{ 
					"verify_code",
					_mongoDbTokenService.Generate(
					logId: _logId,
					id: getId(mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber),
					validityInSeconds: 120,
					numberOfDigits: _telesignInitializer.TelesignSettings.OtpLength).Result
				}
			};
			RestClient.TelesignResponse telesignResponse = _telesignInitializer.VerifyClient.Sms(mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber, parameters);

			return new SMSwitchResponseSendOTP() {
				IsSent = telesignResponse.OK,
				OtpLength = _telesignInitializer.TelesignSettings.OtpLength
			};
		}

		private TokenIdentifier getId(string countryPhoneCodeAndPhoneNumber)
		{
			return $"{_logId}_{countryPhoneCodeAndPhoneNumber}";
		}

		public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			var isValid = _mongoDbTokenService.Validate(id: getId(mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber), token: OTP).Result;
			if (isValid)
			{
				_ = _mongoDbTokenService.Consume(getId(mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber));
			}
			return isValid;
		}
	}
}
