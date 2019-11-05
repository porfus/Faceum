using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videoFaceExtraction
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mtcnn = new MtCnn.MtCnn();
            MtCnn.MtCnn.FaceImgFolderBasePath = @"faces/";

            var filename = @"C:\Users\Nikolas\Downloads\OR03042_Donkovzeva_5_Podezd_2_1570299304_145.mp4";

            var ffMpeg = new NReco.VideoConverter.FFMpegConverter();

            var ffProbe = new NReco.VideoInfo.FFProbe();
            var fileInfo = ffProbe.GetMediaInfo(filename);
            var frameRate = 1000; //ms
            for (var i = 0; i < fileInfo.Duration.TotalMilliseconds; i+=frameRate)
            {
                var newFilename = $"imj_{i}.jpg";
                ffMpeg.GetVideoThumbnail(filename, newFilename, i/1000f);
                var faceProcessed = mtcnn.DetectFaceFromFile(newFilename, (index) =>
                {
                    var filename1 = $"face_{i}_{index}.jpg";
                    return new MtCNN.FilenameToSaveModel { FullFileName = filename1, ShortFileName = filename1 };
                });
                
                File.Delete(newFilename);
                if(faceProcessed != null && faceProcessed.faces.Count>0)
                {
                    frameRate = 150;
                }
                else
                {
                    frameRate = 1000;
                }
            }

        }
    }
}
