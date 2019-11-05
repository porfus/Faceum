using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace FacesVkTestLoader.Model
{
    public class PeopleVkModel
    {
        [BsonId]
        public long Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public int CityId { get; set; }
        public ushort Age { get; set; }
        public ushort BirthMonth { get; set; }
    }
}
