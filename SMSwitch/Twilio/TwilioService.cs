using HumanLanguages;
using Microsoft.Extensions.Logging;
using SMSwitch.DTOs;
using Twilio.Rest.Verify.V2.Service;

namespace SMSwitch.Twilio
{
    public sealed class TwilioService : IServiceMobileNumbers
    {
        private readonly SMSwitchService _smSwitchService;
        private readonly ILogger<TwilioService> _logger;

        public TwilioService(SMSwitchService smSwitchService, ILogger<TwilioService> logger)
        {
            _smSwitchService = smSwitchService;
            _logger = logger;
        }

        public bool SendOTP(MobileNumber mobileWithCountryCode, LanguageId[] languageISOCodeList, string? appHash)
        {
            var locale = languageISOCodeList.First().ToString();
            try
            {
                var verification = VerificationResource.Create(
                    to: $"+{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber}",
                    channel: "sms",
                    locale: locale,
                    pathServiceSid: _smSwitchService.TwilioServiceSid,
                    appHash: appHash
                );

                _logger.LogInformation($"OTP sent to +{mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber} status: {verification.Status}");
                return !string.IsNullOrEmpty(verification?.Sid);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Could not send OTP to {mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber} in {locale}");
                return false;
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
                    pathServiceSid: _smSwitchService.TwilioServiceSid
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
