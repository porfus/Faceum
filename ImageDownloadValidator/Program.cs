using MongoDB.Driver;
using MtcnnNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloadValidator
{
    class Program
    {
        static void Main(string[] args)
        {

            var q1 = Builders<PeopleModel>.Filter.Regex(x => x.UserCity, new MongoDB.Bson.BsonRegularExpression("Орен"));
            var q2 = Builders<PeopleModel>.Filter.Eq(x => x.Photos, null);
            var q3 = Builders<PeopleModel>.Filter.Not(q2);
            var q = Builders<PeopleModel>.Filter.And(q1, q3);
            Console.WriteLine("Start db connection");
            int totalPhotoProcessed = 0;

            string connectionString = "mongodb://79.143.30.220:27088";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            var _collectionPeoples = _db.GetCollection<PeopleModel>("peoples");
            var _collectionPhoto = _db.GetCollection<PhotoModel>("photos");
            var photoToProcessing = new List<string>();
            foreach (var people in _collectionPeoples.Find(q).ToEnumerable())
            {
                foreach(var photo in people.Photos)
                {
                    totalPhotoProcessed++;
                    if(string.IsNullOrEmpty(photo))
                    {
                        continue;
                    }
                    var qq = Builders<PhotoModel>.Filter.Eq(x => x.photo, photo);
                    
                    var processedPhotoInfo =  _collectionPhoto.Find(qq).FirstOrDefault();
                    if(processedPhotoInfo==null)
                    {
                        photoToProcessing.Add(photo);
                        continue;
                    }
                    if(processedPhotoInfo.faces == null || processedPhotoInfo.faces.Count==0)
                    {
                        continue;
                    }
                    

                    var shortFilename = string.Format(MtCnn.MtCnn.GetFilenameFormaFromMd5(photo), 0);
                    var fullFilename = string.Format(MtCnn.MtCnn.GetImageSaveFilename(shortFilename), 0);
                    var ffff = Path.GetFullPath(fullFilename);

                    if(!File.Exists(fullFilename))
                    {

                    }
                    else
                    {

                    }

                }
            }
            
        }
    }
}
