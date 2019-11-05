using FacesVkTestLoader.Model;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FacesVkTestLoader
{
    public class CreateTaskFileToFaceprocessing
    {
        private IMongoCollection<PeopleModel> _collection;
        private IMongoCollection<FaceProcessingResultModel> _collectionPhotos;

        public CreateTaskFileToFaceprocessing()
        {
            string connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            _collection = _db.GetCollection<PeopleModel>("peoples");
            _collectionPhotos = _db.GetCollection<FaceProcessingResultModel>("photos");
        }

        public void Run()
        {
            var output = new List<string>();
            foreach (var photus in _collection.Find(x => true).ToEnumerable().Select(x => x.Photos))
            {
                foreach(var photo in photus)
                {
                    var q = Builders<FaceProcessingResultModel>.Filter.Eq(x => x.url, photo);
                    var hasProcessed = _collectionPhotos.Find(q).Limit(1).CountDocuments() > 0;
                    if (hasProcessed) continue;
                    output.Add(photo);
                }
                
                if (output.Count > 100000) break;
            }
            File.WriteAllLines("taskToFaceProcessing.txt", output);
        }

    }
}
