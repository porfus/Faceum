using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceInferiance;

namespace GetFaceEmmbeddings
{
    class Program
    {
        static void Main(string[] args)
        {
            var fi = new FaceInferiance.FaceInferiance();
            fi.GetFaceEmmbeddins(@"images/0_0.jpg", @"images/1_1.jpg", @"images/1_0.jpg");
        }
    }
}
