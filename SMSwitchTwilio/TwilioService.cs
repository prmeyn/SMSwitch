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

        public SMSwitchResponseSendOTP SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, UserAgent userAgent)
        {
            var locale = languageISOCodeList.First().ToString();
            try
            {
                var verification = VerificationResource.Create(
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

        public bool SendSMS(MobileNumber mobileWithCountryCode, string shortMessageServiceMessage)
        {
            throw new NotImplementedException();
        }

        public bool VerifyOTP(MobileNumber mobileWithCountryCode, string OTP)
        {
            bool verified = false;
            try
            {
                var verification = VerificationCheckResource.Create(
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
