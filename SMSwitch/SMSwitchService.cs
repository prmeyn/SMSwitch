using HumanLanguages;
using SMSwitch.Database;
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



		public SMSwitchService(
			SMSwitchInitializer smSwitchInitializer,
			TwilioService twilioService,
			TelesignService telesignService,
			PlivoService plivoService,
			SMSwitchDbService smSwitchDbService
			)
		{
			_smSwitchInitializer = smSwitchInitializer;
			_twilioService = twilioService;
			_telesignService = telesignService;
			_plivoService = plivoService;
			_smSwitchDbService = smSwitchDbService;
		}

		public async Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, UserAgent userAgent)
		{
			var expiryTimeUtc = DateTimeOffset.UtcNow.AddSeconds(_smSwitchInitializer.SmsControls.SessionTimeoutInSeconds);
			var session = await _smSwitchDbService.GetOrCreateAndGetLatestSession(mobileWithCountryCode, expiryTimeUtc);

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


			SMSwitchResponseSendOTP responseSendOTP = null;

			while (smsProvidersQueue.Count > 0)
			{

				responseSendOTP = smsProvidersQueue.Peek() switch
				{
					SmsProvider.Twilio => await _twilioService.SendOTP(mobileWithCountryCode, languageISOCodeList, userAgent),
					SmsProvider.Plivo => await _plivoService.SendOTP(mobileWithCountryCode, languageISOCodeList, userAgent),
					SmsProvider.Telesign => await _telesignService.SendOTP(mobileWithCountryCode, languageISOCodeList, userAgent),
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

			return responseSendOTP;
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
				}
				return mobileNumberVerified;
			}
			return false;
		}
	}
}
