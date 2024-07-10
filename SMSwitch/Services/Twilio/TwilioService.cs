using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitch.Common;
using SMSwitch.Common.DTOs;
using System;
using Twilio.Rest.Verify.V2.Service;

namespace SMSwitch.Services.Twilio
{
	public sealed class TwilioService : IServiceMobileNumbers
    {
        private readonly TwilioInitializer _twilioInitializer;
		private readonly ILogger<TwilioService> _logger;



        public TwilioService(TwilioInitializer twilioInitializer, ILogger<TwilioService> logger)
        {
            _logger = logger;
            _twilioInitializer = twilioInitializer;
            
        }

        private HashSet<LanguageIsoCode> _supportedLanguageIsoCodesForVerifyDefaultTemplate => new() //https://www.twilio.com/docs/verify/supported-languages#verify-default-template
        {
			HumanHelper.CreateLanguageIsoCode("af"),
            HumanHelper.CreateLanguageIsoCode("ar"),
            HumanHelper.CreateLanguageIsoCode("ca"),
            HumanHelper.CreateLanguageIsoCode("zh"),
            HumanHelper.CreateLanguageIsoCode("hr"),
            HumanHelper.CreateLanguageIsoCode("cs"),
            HumanHelper.CreateLanguageIsoCode("da"),
            HumanHelper.CreateLanguageIsoCode("nl"),
            HumanHelper.CreateLanguageIsoCode("en"),
            HumanHelper.CreateLanguageIsoCode("et"),
            HumanHelper.CreateLanguageIsoCode("fi"),
            HumanHelper.CreateLanguageIsoCode("fr"),
            HumanHelper.CreateLanguageIsoCode("de"),
            HumanHelper.CreateLanguageIsoCode("el"),
            HumanHelper.CreateLanguageIsoCode("he"),
            HumanHelper.CreateLanguageIsoCode("hi"),
            HumanHelper.CreateLanguageIsoCode("hu"),
            HumanHelper.CreateLanguageIsoCode("id"),
            HumanHelper.CreateLanguageIsoCode("it"),
            HumanHelper.CreateLanguageIsoCode("ja"),
            HumanHelper.CreateLanguageIsoCode("kn"),
            HumanHelper.CreateLanguageIsoCode("ko"),
            HumanHelper.CreateLanguageIsoCode("lt"),
            HumanHelper.CreateLanguageIsoCode("ms"),
            HumanHelper.CreateLanguageIsoCode("mr"),
            HumanHelper.CreateLanguageIsoCode("nb"),
            HumanHelper.CreateLanguageIsoCode("pl"),
            HumanHelper.CreateLanguageIsoCode("pt"),
            HumanHelper.CreateLanguageIsoCode("ro"),
            HumanHelper.CreateLanguageIsoCode("ru"),
            HumanHelper.CreateLanguageIsoCode("sk"),
            HumanHelper.CreateLanguageIsoCode("es"),
            HumanHelper.CreateLanguageIsoCode("sv"),
            HumanHelper.CreateLanguageIsoCode("tl"),
            HumanHelper.CreateLanguageIsoCode("te"),
            HumanHelper.CreateLanguageIsoCode("th"),
            HumanHelper.CreateLanguageIsoCode("tr"),
            HumanHelper.CreateLanguageIsoCode("uk"),
            HumanHelper.CreateLanguageIsoCode("vi"),
            HumanHelper.CreateLanguageIsoCode("pt-BR"),
            HumanHelper.CreateLanguageIsoCode("zh-CN"),
            HumanHelper.CreateLanguageIsoCode("zh-HK")
		};

		public async Task<SMSwitchResponseSendOTP> SendOTP(MobileNumber mobileWithCountryCode, HashSet<LanguageIsoCode> preferredLanguageIsoCodeList, UserAgent userAgent)
        {
            var locale = preferredLanguageIsoCodeList.FirstOrDefault(l => _supportedLanguageIsoCodesForVerifyDefaultTemplate.Contains(l))?.ToIsoCodeString() ?? "en";
            try
            {
                var verification = await VerificationResource.CreateAsync(
                    to: $"+{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}",
                    channel: "sms",
                    locale: locale,
                    pathServiceSid: _twilioInitializer.TwilioSettings.TwilioPrivateSettings.ServiceSid,
                    appHash: userAgent == UserAgent.Android ? _twilioInitializer.TwilioSettings.AndroidAppHash : null
				);

                return new SMSwitchResponseSendOTP() { 
                    IsSent = !string.IsNullOrEmpty(verification?.Sid),
                    OtpLength = _twilioInitializer.TwilioSettings.OtpLength
				};
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Could not send OTP to +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber} in {locale}");
                return new SMSwitchResponseSendOTP() {
                    IsSent = false
                };
            }
        }

        public async Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
        {
            throw new NotImplementedException();
        }

        public async Task<SMSwitchResponseVerifyOTP> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
        {
            bool verified = false;
            try
            {
                var verification = await VerificationCheckResource.CreateAsync(
                    to: $"+{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}",
                    code: OTP,
                    pathServiceSid: _twilioInitializer.TwilioSettings.TwilioPrivateSettings.ServiceSid
                );
                verified = verification?.Status.Equals("approved") ?? false;

                if (!verified)
                {
					_logger.LogInformation($"Verification Status: {verification?.Status} for +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
				}
            }
            catch (Exception exception)
            {
				_logger.LogError(exception, $"Could not verify OTP for +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
				return new SMSwitchResponseVerifyOTP()
				{
					Verified = verified,
                    Expired = true
				};
			}
            return new SMSwitchResponseVerifyOTP() {
                Verified = verified
            };
        }
    }
}
