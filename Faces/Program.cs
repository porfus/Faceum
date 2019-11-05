using FacesVkTestLoader;
using System;
using System.Linq;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model;

namespace Faces
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var outputprocessor = new processOutputFile();
            //outputprocessor.Run();
            //var taskCreator = new CreateTaskFileToFaceprocessing();
            //taskCreator.Run();
             var downloader = new Downloader();
            await downloader.Run(true);

            //var vkDownloader = new VkListDownloader();
            //vkDownloader.Download();

        }
    }
}
