using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitchCommon;
using SMSwitchCommon.DTOs;
using Twilio.Rest.Verify.V2.Service;

namespace SMSwitchTwilio
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

        private HashSet<LanguageIsoCode> SupportedLanguageIsoCodesForVerifyDefaultTemplate => new() //https://www.twilio.com/docs/verify/supported-languages#verify-default-template
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
            var locale = preferredLanguageIsoCodeList.FirstOrDefault(l => SupportedLanguageIsoCodesForVerifyDefaultTemplate.Contains(l))?.ToIsoCodeString() ?? "en";
            try
            {
                var verification = await VerificationResource.CreateAsync(
                    to: $"+{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}",
                    channel: "sms",
                    locale: locale,
                    pathServiceSid: _twilioInitializer.TwilioSettings.TwilioPrivateSettings.ServiceSid,
                    appHash: userAgent == UserAgent.Android ? _twilioInitializer.TwilioSettings.AndroidAppHash : null
				);

                _logger.LogInformation($"OTP sent to +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber} status: {verification.Status}");
                return new SMSwitchResponseSendOTP() { 
                    IsSent = !string.IsNullOrEmpty(verification?.Sid),
                    OtpLength = _twilioInitializer.TwilioSettings.OtpLength
				};
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Could not send OTP to {mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber} in {locale}");
                return new SMSwitchResponseSendOTP() {
                    IsSent = false
                };
            }
        }

        public async Task<bool> SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
        {
            bool verified = false;
            try
            {
                var verification = await VerificationCheckResource.CreateAsync(
                    to: $"+{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}",
                    code: OTP,
                    pathServiceSid: _twilioInitializer.TwilioSettings.TwilioPrivateSettings.ServiceSid
                );
                verified = verification?.Valid ?? false;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Verification of OTP failed for +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}");
            }
            return verified;
        }
    }
}
