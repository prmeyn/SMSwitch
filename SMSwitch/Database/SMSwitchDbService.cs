using MongoDB.Driver;
using MongoDbService;
using SMSwitch.Database.DTOs;
using SMSwitchCommon.DTOs;

namespace SMSwitch.Database
{
	public sealed class SMSwitchDbService
	{
		private IMongoCollection<SMSwitchSession> _smSwitchSessionCollection;
		public SMSwitchDbService(MongoService mongoService) 
		{
			_smSwitchSessionCollection = mongoService.Database.GetCollection<SMSwitchSession>(nameof(SMSwitchSession), new MongoCollectionSettings() { ReadConcern = ReadConcern.Majority, WriteConcern = WriteConcern.WMajority });
			
			// Create an index on CountryPhoneCodeAndPhoneNumber
			var indexKeys = Builders<SMSwitchSession>.IndexKeys.Ascending(x => x.CountryPhoneCodeAndPhoneNumber);
			var indexModel = new CreateIndexModel<SMSwitchSession>(indexKeys);
			_ = _smSwitchSessionCollection.Indexes.CreateOneAsync(indexModel);
		}

		private FilterDefinition<SMSwitchSession> Filter(MobileNumber mobileWithCountryCode) => Builders<SMSwitchSession>.Filter.Eq(t => t.CountryPhoneCodeAndPhoneNumber, mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber);
		private FilterDefinition<SMSwitchSession> Filter(string sessionId) => Builders<SMSwitchSession>.Filter.Eq(t => t.SessionId, sessionId);
		internal async Task<SMSwitchSession> GetOrCreateAndGetLatestSession(MobileNumber mobileWithCountryCode, DateTimeOffset expiryTimeUTC)
		{
			var latestSession = await GetLatestSession(mobileWithCountryCode);
			if (latestSession != null && latestSession.HasNotExpired())
			{
				return latestSession;
			}
			latestSession = new SMSwitchSession()
			{
				SessionId = Guid.NewGuid().ToString(),
				CountryPhoneCodeAndPhoneNumber = mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber,
				StartTimeUTC = DateTimeOffset.UtcNow,
				ExpiryTimeUTC = expiryTimeUTC
			};

			await _smSwitchSessionCollection.InsertOneAsync(latestSession);

			return latestSession;
		}

		internal async Task UpdateSession(SMSwitchSession session)
		{
			var options = new ReplaceOptions { IsUpsert = true };
			await _smSwitchSessionCollection.ReplaceOneAsync(Filter(session.SessionId), session, options);
		}

		internal async Task<SMSwitchSession?> GetLatestSession(MobileNumber mobileWithCountryCode)
		{
			var allRecords = _smSwitchSessionCollection.Find(Filter(mobileWithCountryCode));

			if (allRecords?.Any() ?? false)
			{
				var list = allRecords.ToList();
				return list
				.OrderByDescending(record => record.ExpiryTimeUTC)
				.FirstOrDefault();
			}
			return null;
		}
	}
}
