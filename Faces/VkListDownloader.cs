using FacesVkTestLoader.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace FacesVkTestLoader
{
    public class VkListDownloader
    {
        private IMongoCollection<PeopleVkModel> _collection;

        private VkApi api;

        public VkListDownloader()
        {
            string connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            _collection = _db.GetCollection<PeopleVkModel>("vkPeopleList");

            api = new VkApi();

            api.Authorize(new ApiAuthParams
            {
                ApplicationId = 7108376,
                Login = "gora60@ya.ru",
                Password = "LBdKLnzjH6eoarl1cAKK",
                Settings = Settings.All
            });

        }


        public void Download()
        {
            for (ushort age = 17; age < 45; age++)
                for (ushort month = 1; month <= 12; month++)
                {
                    if (ParseUsers(age, month, null, null) < 500) continue;
                    for (var gender = 0; gender <= 2; gender++)                   
                    {

                        if (ParseUsers(age, month, gender, null) < 500) continue;
                        for (var familyStatus = 1; familyStatus <= 7; familyStatus++)
                        {
                            ParseUsers(age, month, gender, familyStatus);
                            
                        }
                    }
                }
        }

        private int ParseUsers(ushort age, ushort month, int? gender, int? familyStatus)
        {

            var reqParam = new UserSearchParams
            {
                AgeFrom = age,
                AgeTo = age,
                BirthMonth = month,
                City = 106,
                Count = 1000,
                HasPhoto=false
            };

            
            if(gender.HasValue)
            {
                reqParam.Sex = (VkNet.Enums.Sex)gender;
            }
            if(familyStatus.HasValue)
            {
                reqParam.Status = (VkNet.Enums.MaritalStatus)familyStatus;
            }
            var users = api.Users.Search(reqParam);

            ProcessUsers(age, month, users);
            Thread.Sleep(TimeSpan.FromSeconds(20));
            return users.Count;


        }

        private void ProcessUsers(ushort age, ushort month, VkNet.Utils.VkCollection<User> users)
        {
            if (users.Count == 1000)
            {

            }
            var inserted = 0;
            foreach (var user in users)
            {
                var people = new PeopleVkModel
                {
                    Age = age,
                    BirthMonth = month,
                    CityId = 106,
                    Family = user.LastName,
                    Name = user.FirstName,
                    Id = user.Id
                };

                try
                {
                    var filter=Builders<PeopleVkModel>.Filter.Eq(x => x.Id, people.Id);
                    var count=_collection.Find(filter).CountDocuments();
                    if (count == 0)
                    {
                        _collection.InsertOne(people);
                        inserted++;
                    }
                   
                }
                catch
                {

                }

            }
            Console.WriteLine($"{users.Count}: {inserted} \t\t\tAge:{age} Month:{month}");
        }
    }

}
