﻿using EarthCountriesInfo;
using Microsoft.Extensions.Configuration;
using SMSwitchCommon;

namespace Countries
{
	public sealed class CountryInitializer
	{
		public readonly HashSet<CountryIsoCode> SupportedCountries;
		public CountryInitializer(IConfiguration configuration)
		{
			SupportedCountries = configuration.GetSection(ConstantStrings.SMSwitchSettingsName)
				?.GetRequiredSection("SupportedCountriesIsoCodes")
				?.Get<string[]>()
				?.Select(c => Enum.Parse<CountryIsoCode>(c))
				?.ToHashSet() ?? [];
		}
	}
}
