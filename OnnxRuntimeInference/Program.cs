using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Numerics.Tensors;

namespace OnnxRuntimeInference
{
    class Program
    {
        static void Main(string[] args)
        {
            var session = new InferenceSession("resnet100.onnx");
            var inputsInfo = session.InputMetadata;

            //Tensor<float> t1, t2;  // let's say data is fed into the Tensor objects
            //var inputs = new List<NamedOnnxValue>()
            // {
            //    NamedOnnxValue.CreateFromTensor<float>("name1", t1),
            //    NamedOnnxValue.CreateFromTensor<float>("name2", t2)
            // };
            //using (var results = session.Run(inputs))
            //{
            //    // manipulate the results
            //}
        }
    }
}
