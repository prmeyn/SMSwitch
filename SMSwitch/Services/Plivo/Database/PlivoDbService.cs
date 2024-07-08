using MongoDB.Driver;
using MongoDbService;
using SMSwitch.Common.DTOs;
using SMSwitch.Services.Plivo.Database.DTOs;

namespace SMSwitch.Services.Plivo.Database
{
	public sealed class PlivoDbService
	{
		private IMongoCollection<PlivoSession> _plivoSessionCollection;

		public PlivoDbService(MongoService mongoService)
		{
			_plivoSessionCollection = mongoService.Database.GetCollection<PlivoSession>(nameof(PlivoSession), new MongoCollectionSettings() { ReadConcern = ReadConcern.Majority, WriteConcern = WriteConcern.WMajority });
		}

		internal async Task SetLatestSessionUUID(MobileNumber mobileWithCountryCode, string sessionUUID)
		{
			var filter = getFilter(mobileWithCountryCode);
			var options = new ReplaceOptions { IsUpsert = true };
			await _plivoSessionCollection.ReplaceOneAsync(filter, new PlivoSession() { CountryPhoneCodeAndPhoneNumber = mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber, SessionUUID = sessionUUID, TimeStamp = DateTimeOffset.UtcNow }, options);
		}

		private FilterDefinition<PlivoSession> getFilter(MobileNumber mobileWithCountryCode)
		{
			var idAsString = mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber;
			return Builders<PlivoSession>.Filter.Eq(t => t.CountryPhoneCodeAndPhoneNumber, idAsString);
		}

		internal async Task<string> GetLatestSessionUUID(MobileNumber mobileWithCountryCode)
		{
			var filter = getFilter(mobileWithCountryCode);
			var sessionInDb = await _plivoSessionCollection.Find(filter).FirstOrDefaultAsync();
			return sessionInDb.SessionUUID;
		}

		internal async Task ClearSessionUUID(MobileNumber mobileWithCountryCode)
		{
			var filter = getFilter(mobileWithCountryCode);
			await _plivoSessionCollection.DeleteManyAsync(filter);
		}
	}
}
