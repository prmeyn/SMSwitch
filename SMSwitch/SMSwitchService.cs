using Countries;
using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitch.Database;
using SMSwitch.Database.DTOs;
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

		private readonly SMSwitchDbService _smSwitchDbService;

		private readonly CountryDbService _countryDbService;

		private readonly ILogger<SMSwitchService> _logger;


		public SMSwitchService(
			SMSwitchInitializer smSwitchInitializer,
			TwilioService twilioService,
			TelesignService telesignService,
			PlivoService plivoService,
			SMSwitchDbService smSwitchDbService,
			CountryDbService countryDbService,
			ILogger<SMSwitchService> logger
			)
		{
			_smSwitchInitializer = smSwitchInitializer;
			_twilioService = twilioService;
			_telesignService = telesignService;
			_plivoService = plivoService;
			_smSwitchDbService = smSwitchDbService;
			_countryDbService = countryDbService;
			_logger = logger;
		}

		public async Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent)
		{
			SMSwitchResponseSendOTP responseSendOTP = null;
			SMSwitchSession session = null;
			try 
			{
				session = await _smSwitchDbService.GetOrCreateAndGetLatestSession(mobileWithCountryCode);

				Queue<SmsProvider> smsProvidersQueue = null;
				if (session.SmsProvidersQueue?.Any() ?? false)
				{
					smsProvidersQueue = session.SmsProvidersQueue;
				}
				else
				{
					smsProvidersQueue = new();
					HashSet<SmsProvider> smsProviders = null;
					if (!_smSwitchInitializer.SmsControls.PriorityBasedOnCountryPhoneCode.TryGetValue(mobileWithCountryCode.CountryPhoneCodeAsNumericString, out smsProviders))
					{
						smsProviders = _smSwitchInitializer.SmsControls.FallBackPriority;
					}
					for (int i = 0; i < _smSwitchInitializer.SmsControls.MaxRoundRobinAttempts; i++)
					{
						foreach (SmsProvider smsProvider in smsProviders)
						{
							smsProvidersQueue.Enqueue(smsProvider);
						}
					}
				}

				if (smsProvidersQueue.Count == 0)
				{
					return new SMSwitchResponseSendOTP()
					{
						IsSent = false
					};
				}

				while (smsProvidersQueue.Count > 0)
				{

					responseSendOTP = smsProvidersQueue.Peek() switch
					{
						SmsProvider.Twilio => await _twilioService.SendOTP(mobileWithCountryCode, preferredLanguageIsoCodeList, userAgent),
						SmsProvider.Plivo => await _plivoService.SendOTP(mobileWithCountryCode, preferredLanguageIsoCodeList, userAgent),
						SmsProvider.Telesign => await _telesignService.SendOTP(mobileWithCountryCode, preferredLanguageIsoCodeList, userAgent),
						_ => throw new NotImplementedException(),
					};

					if (responseSendOTP.IsSent)
					{
						break;
					}
					else
					{
						smsProvidersQueue.Dequeue();
					}
				}

				session.SmsProvidersQueue = smsProvidersQueue;
				await _smSwitchDbService.UpdateSession(session);

				if (responseSendOTP == null || !responseSendOTP.IsSent)
				{
					_logger.LogCritical($"Unable to send OTP to {mobileWithCountryCode} with SessionId{session?.SessionId}");
				}
			}
			catch ( Exception exception) 
			{
				_logger.LogCritical(exception, $"Unable to send OTP to {mobileWithCountryCode} with SessionId{session?.SessionId}");
			}
			
			return responseSendOTP ?? new SMSwitchResponseSendOTP() { IsSent = false }; 
		}

		public async Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			var session = await _smSwitchDbService.GetLatestSession(mobileWithCountryCode);

			if (session?.SmsProvidersQueue?.Any() ?? false)
			{
				var mobileNumberVerified =  session.SmsProvidersQueue.Peek() switch
				{
					SmsProvider.Twilio => await _twilioService.VerifyOTP(mobileWithCountryCode, OTP),
					SmsProvider.Plivo => await _plivoService.VerifyOTP(mobileWithCountryCode, OTP),
					SmsProvider.Telesign => await _telesignService.VerifyOTP(mobileWithCountryCode, OTP),
					_ => throw new NotImplementedException(),
				};

				if (mobileNumberVerified)
				{
					session.SuccessfullyVerifiedTimestampUTC = DateTimeOffset.UtcNow;
					await _smSwitchDbService.UpdateSession(session);
					_ = _countryDbService.FeedbackAsync(
						countryPhoneCode: mobileWithCountryCode.CountryPhoneCodeAsNumericString,
						phoneNumberLength: (byte)mobileWithCountryCode.PhoneNumberAsNumericString.Length,
						countryIsoCode: mobileWithCountryCode.CountryIsoCode);
				}
				return mobileNumberVerified;
			}
			return false;
		}
	}
}
