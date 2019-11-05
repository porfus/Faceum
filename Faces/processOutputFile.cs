using FacesVkTestLoader.Model;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FacesVkTestLoader
{
    public class processOutputFile
    {
        private IMongoCollection<FaceProcessingResultModel> _collection;

        public processOutputFile()
        {
            string connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            _collection = _db.GetCollection<FaceProcessingResultModel>("photos");
        }
        public void Run()
        {
            var output=File.ReadAllLines("output.txt");
            var results = output.Select(x => JsonConvert.DeserializeObject<FaceProcessingResultModel>(x));
            foreach(var res in results)
            {
                _collection.InsertOne(res);
            }
        }
    }
}
