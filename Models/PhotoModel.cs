using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MtcnnNet
{
    public class Face
    {
        public List<double> box { get; set; }
        public string filename { get; set; }
    }

    public class PhotoModel
    {
        [BsonId]
        public ObjectId _ig { get; set; }
        public string photo { get; set; }
        public List<Face> faces { get; set; } = new List<Face>();
    }

}
