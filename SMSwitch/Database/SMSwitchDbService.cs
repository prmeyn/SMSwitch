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

		internal SMSwitchSession GetLatestSession(MobileNumber mobileWithCountryCode, DateTimeOffset expiryTimeUTC)
		{
			var allRecords = _smSwitchSessionCollection.Find(Filter(mobileWithCountryCode)).ToList();

			var x = allRecords.Where(record => record.ExpiryTimeUTC <= expiryTimeUTC).ToList();
			return null; // todo
		}
	}
}
