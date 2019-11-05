using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class EmbeddingFaceModel
    {
        public string FaceId { get; set; }
        public float[] Embedding { get; set; }
    }
}
