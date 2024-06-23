using HumanLanguages;
using MongoDB.Bson.Serialization.Attributes;

namespace Countries.Database.DTOs
{
	public sealed class CountryInfo
	{
		[BsonId]
		public required string CountryCode { get; init; }
		public Dictionary<string, string>? CountryNames { get; set; }
		public required string CountryPhoneCode { get; set; }
		public Dictionary<string, string>? ValidLengthsAndFormat { get; set; }
		public bool IsSupported { get; set; }
		public string GetCountryName(LanguageId languageIsoCode) => CountryNames != null && CountryNames.TryGetValue(languageIsoCode.ToString(), out var countryName) ? countryName : "";
		public Dictionary<int, string> GetValidLengthsAndFormat() => ValidLengthsAndFormat == null ? new() : ValidLengthsAndFormat.ToDictionary(k => int.Parse(k.Key), k => k.Value);
	}
}
