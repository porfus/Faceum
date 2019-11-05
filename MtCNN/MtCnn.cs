using MtCNN;
using MtcnnNet;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MtCnn
{
    public class MtCnn:IDisposable
    {
        static dynamic cv2;
        static dynamic detector;
        static dynamic np;
        static dynamic code;
        public static string FaceImgFolderBasePath = @"../../../facedb";
        private Py.GILState state;

        public MtCnn()
        {
            //PythonEngine.PythonPath = @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python37_64\python.exe";
            Console.WriteLine(PythonEngine.Version);
            Console.WriteLine(PythonEngine.PythonPath);
            state=Py.GIL();

            PythonEngine.Exec(@"import sys
sys.path.insert(0, '/content/MtcnnNet/')");
            np = Py.Import("numpy");
            code = Py.Import("codeMy");
            dynamic mtcnn = Py.Import("mtcnn.mtcnn");
            detector = mtcnn.MTCNN(min_face_size: 25);
            cv2 = Py.Import("cv2");
        }

        

        public PhotoModel DetectFaceFromFile(string filename, Func<int, FilenameToSaveModel> getFullFaceFilename)
        {
            var img = cv2.imread(filename);
            if (img != null)
            {
                // img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB);

                int srcImageWidth = img.shape[0].As<int>();
                int srcImageHeight = img.shape[1].As<int>();
                var minSrcImageSize = Math.Min(srcImageHeight, srcImageWidth);
                var scalefactor = 200f / minSrcImageSize;
                if (scalefactor > 1) scalefactor = 1;
                else if (scalefactor > 0.3) scalefactor = 0.3f;
                var newWidth = new PyInt((int)(srcImageWidth * scalefactor));
                var newHeight = new PyInt((int)(srcImageHeight * scalefactor));

                var ffff = new PyTuple(new[] { newHeight, newWidth });

                var imgToFaceDetection = cv2.resize(img, dsize: ffff);
                var newShape = imgToFaceDetection.shape;

                var faces = detector.detect_faces(imgToFaceDetection);

                var photoModel = new PhotoModel();

                var fountFacesCount = (faces as PyObject).Length();
                for (var ee = 0; ee < fountFacesCount; ee++)
                {
                    var confidence = faces[ee]["confidence"].As<float>();
                    if (confidence < 0.98) continue;

                    var LeftEKeyPoint = np.array((faces[ee]["keypoints"]["left_eye"]));
                    var RightEKeyPoint = np.array(faces[ee]["keypoints"]["right_eye"]);

                    var LeftMKeyPoint = np.array(faces[ee]["keypoints"]["mouth_left"]);
                    var RightMKeyPoint = np.array(faces[ee]["keypoints"]["mouth_right"]);

                    var NoseKeyPoint = np.array(faces[ee]["keypoints"]["nose"]);

                    var outKeyPointsPyList = np.array(new List<dynamic> { LeftEKeyPoint, RightEKeyPoint, NoseKeyPoint, LeftMKeyPoint, RightMKeyPoint }) / scalefactor;
                    // var outBBox = new List<int> { boxTop, boxLeft, boxTop + boxW, boxLeft + boxH };


                    var fff = code.preprocess(img, landmark: outKeyPointsPyList);

                    var filenames = new FilenameToSaveModel { FullFileName = ee + ".jpg",ShortFileName =  ee + ".jpg" };
                    if (getFullFaceFilename != null)
                    {
                        filenames = getFullFaceFilename(ee);
                    }
                    
                    
                    cv2.imwrite(filenames.FullFileName, fff);
                    
                    var boxum = ((int[])faces[ee]["box"].As<int[]>()).ToList();
                    photoModel.faces.Add(new Face { box = boxum.Cast<double>().ToList(), filename =string.Format(filenames.ShortFileName, ee)});
                }
                return photoModel;
               
            }
            else
            {
                return null;
            }
        }

        
        
        public static string GetFilenameFormaFromMd5(string input)
        {
            return CreateMD5(input) + "_{0}";
        }

        public static string GetImageSaveFilename(string filenameOut)
        {           
            
            var folder1 = filenameOut[0];
            var folder2 = filenameOut[1];
            var folder3 = filenameOut[2];

            var saveFolderName = Path.GetDirectoryName($"{FaceImgFolderBasePath}/{folder1}/{folder2}/{folder3}/");
            if (!Directory.Exists(saveFolderName))
            {
                Directory.CreateDirectory(saveFolderName);
            }

            return Path.Combine(saveFolderName, filenameOut+ ".jpg");
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public void Dispose()
        {
            state.Dispose();
        }
    }
}
