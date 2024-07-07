using Countries.Database.DTOs;
using EarthCountriesInfo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDbService;
using System.Text;

namespace Countries
{
	public sealed class CountryDbService : IHostedService
	{
		private readonly ILogger<CountryDbService> _logger;
		IMongoDatabase _database;
		private IMongoCollection<CountryInfo> _countryPhoneCodeCollection;

		private CountryInitializer _countryInitializer;



		public CountryDbService(ILogger<CountryDbService> logger, MongoService mongoService, CountryInitializer countryInitializer)
		{
			_logger = logger;
			_countryPhoneCodeCollection = mongoService.Database.GetCollection<CountryInfo>(nameof(CountryInfo), new MongoCollectionSettings() { ReadConcern = ReadConcern.Majority, WriteConcern = WriteConcern.WMajority });
			_countryInitializer = countryInitializer; ;
		}

		public async Task LoadCollectionFromCodeBase()
		{
			if (!(_countryPhoneCodeCollection.EstimatedDocumentCount() > 0))
			{
				foreach (var countryInfo in _dataSource)
				{
					await _countryPhoneCodeCollection.InsertOneAsync(countryInfo);
				}
			}
			else
			{
				var allCountriesFromDb = await GetAllCountriesFromDb();
				foreach (var countryInfoFromDb in allCountriesFromDb)
				{
					var localVersion = _dataSource.FirstOrDefault(c => c.CountryCode == countryInfoFromDb.CountryCode);
					if (NeedsAnUpdateInDb(countryInfoFromDb, localVersion, out CountryInfo latestVersion))
					{
						var options = new ReplaceOptions { IsUpsert = true };
						var filter = Builders<CountryInfo>.Filter.Eq(e => e.CountryCode, countryInfoFromDb.CountryCode);
						await _countryPhoneCodeCollection.ReplaceOneAsync(filter, latestVersion, options);
					}
				}

				var allCountriesFromLocalNotInDB = _dataSource.Where(c => !allCountriesFromDb.Any(cdb => cdb.CountryCode == c.CountryCode));
				foreach (var countryFromLocalNotInDB in allCountriesFromLocalNotInDB)
				{
					if (NeedsAnUpdateInDb(null, countryFromLocalNotInDB, out CountryInfo latestVersion))
					{
						var options = new ReplaceOptions { IsUpsert = true };
						var filter = Builders<CountryInfo>.Filter.Eq(e => e.CountryCode, latestVersion.CountryCode);
						await _countryPhoneCodeCollection.ReplaceOneAsync(filter, latestVersion, options);
					}
				}
			}
		}

		private static bool NeedsAnUpdateInDb(CountryInfo? countryInfoFromDb, CountryInfo? localVersion, out CountryInfo mergedVersion)
		{
			// If localVersion is null, no update is needed
			if (localVersion == null)
			{
				mergedVersion = countryInfoFromDb;
				return false;
			}


			// Start with the database version
			mergedVersion = countryInfoFromDb;

			if (countryInfoFromDb == null)
			{
				mergedVersion = localVersion;
				return true;
			}

			// Check each property for differences
			bool updateNeeded = false;


			if (mergedVersion.CountryPhoneCode != localVersion.CountryPhoneCode)
			{
				mergedVersion.CountryPhoneCode = localVersion.CountryPhoneCode;
				updateNeeded = true;
			}

			if (mergedVersion.IsSupported != localVersion.IsSupported)
			{
				mergedVersion.IsSupported = localVersion.IsSupported;
				updateNeeded = true;
			}

			// For the dictionaries, we'll merge them
			if (localVersion.CountryNames != null)
			{
				foreach (var pair in localVersion.CountryNames)
				{
					if (!mergedVersion.CountryNames.ContainsKey(pair.Key) || mergedVersion.CountryNames[pair.Key] != pair.Value)
					{
						mergedVersion.CountryNames[pair.Key] = pair.Value;
						updateNeeded = true;
					}
				}
			}

			if (localVersion.ValidLengthsAndFormat != null)
			{
				foreach (var pair in localVersion.ValidLengthsAndFormat)
				{
					if (!mergedVersion.ValidLengthsAndFormat.ContainsKey(pair.Key) || mergedVersion.ValidLengthsAndFormat[pair.Key] != pair.Value)
					{
						mergedVersion.ValidLengthsAndFormat[pair.Key] = pair.Value;
						updateNeeded = true;
					}
				}
			}

			return updateNeeded;
		}


		public async Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("CountryDbService running.");

			await LoadCollectionFromCodeBase();
		}

		public Task StopAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("CountryDbService is stopping.");

			return Task.CompletedTask;
		}

		public async Task<List<CountryInfo>> GetAllCountriesFromDb()
		{
			var allCountries = await _countryPhoneCodeCollection.Find(e => true).ToListAsync();

			if (allCountries != null && allCountries.Any())
			{
				return allCountries.ToList();
			}
			return [];
		}

		public async Task FeedbackAsync(string countryPhoneCode, byte phoneNumberLength, CountryIsoCode? countryIsoCode)
		{
			if (countryIsoCode is not null)
			{
				var filter = Builders<CountryInfo>.Filter.Eq(e => e.CountryCode, countryIsoCode.ToString());
				var countryToUpdate = await _countryPhoneCodeCollection.Find(filter).FirstOrDefaultAsync();
				if (countryToUpdate.ValidLengthsAndFormat == null)
				{
					countryToUpdate.ValidLengthsAndFormat = [];
				}
				if (countryToUpdate != null && countryToUpdate.CountryPhoneCode == countryPhoneCode && !countryToUpdate.ValidLengthsAndFormat.TryGetValue(phoneNumberLength.ToString(), out string? _))
				{
					countryToUpdate.ValidLengthsAndFormat.Add(phoneNumberLength.ToString(), ConvertByteToHashString(phoneNumberLength));
					var options = new ReplaceOptions { IsUpsert = true };
					await _countryPhoneCodeCollection.ReplaceOneAsync(filter, countryToUpdate, options);
				}
			}
		}

		private static string ConvertByteToHashString(byte numHashes)
		{
			// Create a string with the specified number of '#' characters
			StringBuilder hashString = new StringBuilder();
			for (int i = 0; i < numHashes; i++)
			{
				hashString.Append('#');
			}

			return hashString.ToString();
		}



		private HashSet<CountryInfo> _dataSource => EarthCountriesInfo.Countries.CountryPropertiesDictionary.Select(c => new CountryInfo
		{
			CountryCode = c.Key.ToString(),
			CountryNames = c.Value.CountryNames.ToDictionary(c => c.Key.ToString(), c => c.Value),
			CountryPhoneCode = c.Value.CountryPhoneCode,
			ValidLengthsAndFormat = c.Value.ValidLengthsAndFormat?.ToDictionary(vl => vl.Key.ToString(), vl => vl.Value),
			IsSupported = (_countryInitializer.SupportedCountries?.Any() ?? false) ? _countryInitializer.SupportedCountries.Contains(c.Key) : true,
		}).ToHashSet();

	}
}
