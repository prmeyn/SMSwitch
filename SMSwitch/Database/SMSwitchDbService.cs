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
		internal SMSwitchSession GetOrCreateAndGetLatestSession(MobileNumber mobileWithCountryCode, DateTimeOffset expiryTimeUTC)
		{
			var latestSession = GetLatestSession(mobileWithCountryCode);
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

			_ = _smSwitchSessionCollection.InsertOneAsync(latestSession);

			return latestSession;
		}

		internal void UpdateSession(SMSwitchSession session)
		{
			var options = new ReplaceOptions { IsUpsert = true };
			_smSwitchSessionCollection.ReplaceOne(Filter(session.SessionId), session, options);
		}

		internal SMSwitchSession GetLatestSession(MobileNumber mobileWithCountryCode)
		{
			var allRecords = _smSwitchSessionCollection.Find(Filter(mobileWithCountryCode)).ToList();

			return allRecords
				.OrderByDescending(record => record.ExpiryTimeUTC)
				.FirstOrDefault();
		}
	}
}
