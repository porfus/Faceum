using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace FaceInferiance
{
    public class FaceInferiance:IDisposable
    {
        private Py.GILState pythonGilState;
        private PyObject module_face;
        private PyObject model;
        private PyObject funcGetImageFaceEmbedding;

        public FaceInferiance()
        {
            pythonGilState = Py.GIL();
            var module_irse = ImportModuleFromResource("model_irse", "model_irse.py");
            module_face = ImportModuleFromResource("faceInfiriance", "FaceInfiriance.py");

            var funcLoadModel = module_face.GetAttr("load_model");
            model = funcLoadModel.Invoke();

            funcGetImageFaceEmbedding =  module_face.GetAttr("get_image_face_embedding");
        }

        public List<float[]> GetFaceEmmbeddins(params string[] imageFilename )
        {
            var inputArgs = new List<PyObject>();
            inputArgs.Add(model);
            inputArgs.AddRange(imageFilename.Select(x => new PyString(x)));

            var result = funcGetImageFaceEmbedding.Invoke(inputArgs.ToArray());

            var output = new List<float[]>();
            for(var i=0;i<imageFilename.Length;i++)
            {
                output.Add(((float[])result[0].As<float[]>()));
            }

            
            return output;
        }

        private PyObject ImportModuleFromResource(string moduleName,string resourceScriptName)
        {
            var assembly = Assembly.GetAssembly(typeof(FaceInferiance));
            var resource = assembly.GetManifestResourceNames();

            using (Stream stream = assembly.GetManifestResourceStream($"FaceInferiance.Scripts.{resourceScriptName}"))
            using (StreamReader reader = new StreamReader(stream))
            {               
                var scriptText = reader.ReadToEnd();
                var module = PythonEngine.ModuleFromString(moduleName, scriptText);
                return module;
            }
        }

        public void Dispose()
        {
            pythonGilState.Dispose();
        }
    }
}
