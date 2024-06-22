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

		public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, bool isAndroidDevice)
		{
			var expiryTimeUtc = DateTimeOffset.UtcNow.AddSeconds(_smSwitchInitializer.SmsControls.SessionTimeoutInSeconds);
			var session = _smSwitchDbService.GetOrCreateAndGetLatestSession(mobileWithCountryCode, expiryTimeUtc);

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
					SmsProvider.Twilio => _twilioService.SendOTP(mobileWithCountryCode, languageISOCodeList, isAndroidDevice),
					SmsProvider.Plivo => _plivoService.SendOTP(mobileWithCountryCode, languageISOCodeList, isAndroidDevice),
					SmsProvider.Telesign => _telesignService.SendOTP(mobileWithCountryCode, languageISOCodeList, isAndroidDevice),
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
			_smSwitchDbService.UpdateSession(session);

			return responseSendOTP;
		}

		public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
		{
			throw new NotImplementedException();
		}

		public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
		{
			var session = _smSwitchDbService.GetLatestSession(mobileWithCountryCode);

			if (session.SmsProvidersQueue?.Any() ?? false)
			{
				var mobileNumberVerified =  session.SmsProvidersQueue.Peek() switch
				{
					SmsProvider.Twilio => _twilioService.VerifyOTP(mobileWithCountryCode, OTP),
					SmsProvider.Plivo => _plivoService.VerifyOTP(mobileWithCountryCode, OTP),
					SmsProvider.Telesign => _telesignService.VerifyOTP(mobileWithCountryCode, OTP),
					_ => throw new NotImplementedException(),
				};
				if (mobileNumberVerified)
				{
					session.SuccessfullyVerifiedTimestampUTC = DateTimeOffset.UtcNow;
					_smSwitchDbService.UpdateSession(session);
				}
				return mobileNumberVerified;
			}
			return false;
		}
	}
}
