using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitch.Common;
using SMSwitch.Common.DTOs;
using SMSwitch.Countries.Database;
using SMSwitch.Database;
using SMSwitch.Database.DTOs;
using SMSwitch.Services.Plivo;
using SMSwitch.Services.Twilio;

namespace SMSwitch
{
	public sealed class SMSwitchService : IServiceMobileNumbers
	{

		private readonly SMSwitchInitializer _smSwitchInitializer;

		private readonly TwilioService _twilioService;
		private readonly PlivoService _plivoService;

		private readonly SMSwitchDbService _smSwitchDbService;

		private readonly CountryDbService _countryDbService;

		private readonly ILogger<SMSwitchService> _logger;


		public SMSwitchService(
			SMSwitchInitializer smSwitchInitializer,
			TwilioService twilioService,
			PlivoService plivoService,
			SMSwitchDbService smSwitchDbService,
			CountryDbService countryDbService,
			ILogger<SMSwitchService> logger
			)
		{
			_smSwitchInitializer = smSwitchInitializer;
			_twilioService = twilioService;
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
						_ => throw new NotImplementedException(),
					};

					session.SentAttempts.Add(new AttemptDetailsSendOTP(DateTimeOffset.UtcNow, smsProvidersQueue.Peek(), responseSendOTP.IsSent));
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
					_logger.LogCritical($"Unable to send OTP to {mobileWithCountryCode} with SessionId: {session?.SessionId}");
				}
			}
			catch ( Exception exception) 
			{
				_logger.LogCritical(exception, $"Unable to send OTP to {mobileWithCountryCode} with SessionId: {session?.SessionId}");
			}
			
			return responseSendOTP ?? new SMSwitchResponseSendOTP() { IsSent = false }; 
		}

		public async Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public async Task<SMSwitchResponseVerifyOTP> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			var session = await _smSwitchDbService.GetLatestSession(mobileWithCountryCode);

			if (session?.SmsProvidersQueue?.Any() ?? false)
			{
				var mobileNumberVerified =  session.SmsProvidersQueue.Peek() switch
				{
					SmsProvider.Twilio => await _twilioService.VerifyOTP(mobileWithCountryCode, OTP),
					SmsProvider.Plivo => await _plivoService.VerifyOTP(mobileWithCountryCode, OTP),
					_ => throw new NotImplementedException(),
				};

				if (mobileNumberVerified.Verified)
				{
					session.SuccessfullyVerifiedTimestampUTC = DateTimeOffset.UtcNow;
					_ = _countryDbService.FeedbackAsync(
						countryPhoneCode: mobileWithCountryCode.CountryPhoneCodeAsNumericString,
						phoneNumberLength: (byte)mobileWithCountryCode.PhoneNumberAsNumericString.Length,
						countryIsoCode: mobileWithCountryCode.CountryIsoCode);
				}
				else
				{
					session.FailedVerificationAttemptsDateTimeOffset.Add(DateTimeOffset.UtcNow);
				}
				await _smSwitchDbService.UpdateSession(session);
				mobileNumberVerified.Expired = !session.HasNotExpired(_smSwitchInitializer.SmsControls.MaximumFailedAttemptsToVerify);
				return mobileNumberVerified;
			}
			if (session is not null)
			{
				session.FailedVerificationAttemptsDateTimeOffset.Add(DateTimeOffset.UtcNow);
				await _smSwitchDbService.UpdateSession(session);
			}
			else 
			{
				_logger.LogInformation($"Session not found: Unable to verify OTP for {mobileWithCountryCode} with OTP: {OTP}");
			}
			return new SMSwitchResponseVerifyOTP() {
				Verified = false,
				Expired = true
			};
		}
	}
}
