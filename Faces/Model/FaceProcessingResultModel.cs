using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace FacesVkTestLoader.Model
{
    public class Face
    {
        public List<double> bbox { get; set; }
        public List<double> embedding { get; set; }
    }

    public class FaceProcessingResultModel
    {
        
        public string url { get; set; }
        public List<Face> faces { get; set; }
    }
}
