using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class EmbeddingFaceModel
    {
        [BsonId]
        public string FaceId { get; set; }
        public float[] Embedding { get; set; }
    }
}
