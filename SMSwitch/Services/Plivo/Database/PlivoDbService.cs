﻿using Microsoft.AspNetCore.Hosting;
using MongoDB.Driver;
using MongoDbService;
using SMSwitch.Common.DTOs;
using SMSwitch.Services.Plivo.Database.DTOs;

namespace SMSwitch.Services.Plivo.Database
{
	public sealed class PlivoDbService
	{
		private IMongoCollection<PlivoSession> _plivoSessionCollection;
		private IHostingEnvironment _hostingEnvironment;
		public PlivoDbService(MongoService mongoService, IHostingEnvironment hostingEnvironment)
		{
			_plivoSessionCollection = mongoService.Database.GetCollection<PlivoSession>(nameof(PlivoSession), new MongoCollectionSettings() { ReadConcern = ReadConcern.Majority, WriteConcern = WriteConcern.WMajority });
			_hostingEnvironment = hostingEnvironment;
		}

		internal async Task SetLatestSessionUUID(MobileNumber mobileWithCountryCode, string sessionUUID)
		{
			var filter = getFilter(mobileWithCountryCode);
			var options = new ReplaceOptions { IsUpsert = true };
			await _plivoSessionCollection.ReplaceOneAsync(filter, new PlivoSession() { CountryPhoneCodeAndPhoneNumber = mobileWithCountryCode.CountryPhoneCodeAndPhoneNumber, SessionUUID = sessionUUID, TimeStamp = DateTimeOffset.UtcNow }, options);
		}

		internal async Task UpdateSessionUUID(string mobileNumberCountryPhoneCodeAndPhoneNumber, string sessionUUID, PlivoNotification plivoNotification)
		{
			var filter = getFilter(mobileNumberCountryPhoneCodeAndPhoneNumber, sessionUUID);
			var sessionInDb = await _plivoSessionCollection.Find(filter).FirstOrDefaultAsync();
			if (sessionInDb is not null)
			{
				sessionInDb.Notifications.Add(plivoNotification);
				var options = new ReplaceOptions { IsUpsert = true };
				await _plivoSessionCollection.ReplaceOneAsync(filter, sessionInDb, options);
			}
		}

		private FilterDefinition<PlivoSession> getFilter(string mobileNumberCountryPhoneCodeAndPhoneNumber, string sessionUUID)
		{
			return Builders<PlivoSession>.Filter.Eq(t => t.CountryPhoneCodeAndPhoneNumber, mobileNumberCountryPhoneCodeAndPhoneNumber) & getFilter(sessionUUID);
		}

		private FilterDefinition<PlivoSession> getFilter(string sessionUUID)
		{
			return Builders<PlivoSession>.Filter.Eq(t => t.SessionUUID, sessionUUID);
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

		internal async Task<bool> KeepCheckingTheDatabaseIfSentEvery2seconds(string sessionUUID, DateTimeOffset expiry)
		{
			if (!_hostingEnvironment.IsProduction())
			{
				return true;
			}
			var filter = getFilter(sessionUUID);
			var sessionInDb = await _plivoSessionCollection.Find(filter).FirstOrDefaultAsync();
			if (sessionInDb is not null)
			{
				if (sessionInDb.Notifications.Any(n => n.channelStatus == "delivered"))
				{
					return true;
				}
				else if (sessionInDb.Notifications.Any(n => n.channelStatus == "failed") || DateTimeOffset.UtcNow >= expiry)
				{
					return false;
				}
				else
				{
					await Task.Delay(TimeSpan.FromSeconds(2));
					return await KeepCheckingTheDatabaseIfSentEvery2seconds(sessionUUID, expiry);
				}
			}
			return false;
		}
	}
}
