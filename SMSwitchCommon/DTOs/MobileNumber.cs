using EarthCountriesInfo;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SMSwitchCommon.DTOs
{
    public sealed class MobileNumber
    {
		[JsonPropertyName("countryIsoCode")] public required string CountryIsoCodeString { get; init; }
		[JsonPropertyName("countryPhoneCode")] public required string CountryPhoneCode { get; set; }
        [JsonPropertyName("phoneNumber")] public required string PhoneNumber { get; set; }
        public CountryIsoCode? CountryIsoCode => Enum.TryParse(CountryIsoCodeString, ignoreCase: true, out CountryIsoCode countryIsoCode) ? countryIsoCode : null;
		private string removeNonNumericString(string input) => Regex.Replace(input, "[^0-9]", "");
        public string CountryPhoneCodeAsNumericString => removeNonNumericString(CountryPhoneCode);
        public string PhoneNumberAsNumericString => removeNonNumericString(PhoneNumber);
        private long removeNonNumeric(string input) => long.Parse(removeNonNumericString(input));
        public string CountryPhoneCodeAndPhoneNumber => $"{removeNonNumeric(CountryPhoneCode)}{removeNonNumeric(PhoneNumber)}";
        public byte PhoneNumberNumericLength() => Convert.ToByte($"{removeNonNumeric(PhoneNumber)}".Length);

        public bool IsValid() => !string.IsNullOrWhiteSpace(CountryPhoneCodeAsNumericString) && !string.IsNullOrWhiteSpace(PhoneNumberAsNumericString);
    }
}
