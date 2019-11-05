using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DriveQuickstart
{
    class Program
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] Scopes = { DriveService.Scope.DriveReadonly, DriveService.Scope.DriveFile, DriveService.Scope.Drive };
        static string ApplicationName = "Drive API .NET Quickstart";
        static string destinationFolder = @"C:\Users\Nikolas\source\repos\Faces\facedb";
        static int totalFileProcessedCount = 0;
        static int SucsefullFileProcessedCount = 0;
        static int RunningThreadCount = 0;
        static int FilenamesRecived = 0;
        static int numOfThreads = 7;
        static int continedFiles = 0;
        private static List<Task> tasks;
        private static ConcurrentQueue<DriveService> googleDriveSevicePool;

        static async Task Main(string[] args)
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 4;
            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            while (true)
            {
                try
                {
                    var result = service.Files.List();
                    result.Q = "mimeType='image/jpeg'";
                    result.PageSize = 1000;
                    result.Spaces = "drive";
                    result.Fields = "nextPageToken, files(id, name)";
                    result.PageToken = null;
                    var files = await result.ExecuteAsync();
                    
                    var req = service.Files.EmptyTrash();
                    var resus = req.Execute();
                    Console.WriteLine(files.Files.Count);
                    Thread.Sleep(TimeSpan.FromSeconds(2));
                    var about = service.About.Get();
                    about.Fields = "storageQuota";
                    var resAbount =  await about.ExecuteAsync();
                    Console.WriteLine(resAbount.StorageQuota.Usage);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }

            }
            return;
            googleDriveSevicePool = new ConcurrentQueue<DriveService>();
            for (var i = 0; i < 50; i++)
            {
                googleDriveSevicePool.Enqueue(new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                }));
            }
            var time = DateTime.Now;
            tasks = new List<Task>();
            String pageToken = null;
            do
            {
                var result = service.Files.List();
                result.Q = "mimeType='image/jpeg'";
                result.PageSize = 1000;
                result.Spaces = "drive";
                result.Fields = "nextPageToken, files(id, name)";
                result.PageToken = pageToken;

                var files = await result.ExecuteAsync();

                foreach (var file in files.Files)
                {
                    FilenamesRecived++;
                    if (file.Name.Length < 36) continue;
                    var folder1 = file.Name[0].ToString();
                    var folder2 = file.Name[1].ToString();
                    var folder3 = file.Name[2].ToString();
                    var newFilename = Path.Combine(destinationFolder, folder1, folder2, folder3, file.Name);
                    if (System.IO.File.Exists(newFilename))
                    {
                        continedFiles++;
                        continue;
                    }
                    while (RunningThreadCount > numOfThreads)
                    {
                        await Task.Delay(1);
                    }
                    RunningThreadCount++;

                    if (googleDriveSevicePool.TryDequeue(out DriveService serviceToTask))
                    {

                        var state = new DownloadState
                        {
                            DriveService = serviceToTask
                        };
                        var task = DownloadFile(file, newFilename, service).ContinueWith(FileDownloadedCallback,state);
                        tasks.Add(task);

                        totalFileProcessedCount++;
                        tasks = tasks.Where(x => x.Status != TaskStatus.RanToCompletion).ToList(); ;
                        if (totalFileProcessedCount % 200 == 0)
                        {
                            RunningThreadCount = tasks.Count;
                            var tmpTime = DateTime.Now;
                            var delta = tmpTime - time;
                            if (delta.TotalMilliseconds < 21000)
                            {

                                System.Net.ServicePointManager.DefaultConnectionLimit--;
                                numOfThreads = (int)(System.Net.ServicePointManager.DefaultConnectionLimit * 1.5);


                            }
                            else
                            {
                                if (System.Net.ServicePointManager.DefaultConnectionLimit < 25)
                                {
                                    System.Net.ServicePointManager.DefaultConnectionLimit++;
                                    numOfThreads = (int)(System.Net.ServicePointManager.DefaultConnectionLimit * 1.5);
                                }
                            }
                            time = tmpTime;
                            Console.WriteLine($"Total: {totalFileProcessedCount} \t Sucsess: {SucsefullFileProcessedCount} \t " +
                                $"ContinedFiles: {continedFiles} \t Delta: {delta.TotalMilliseconds} " +
                                $"FilenamesRecived: {FilenamesRecived}");
                        }
                    }
                    else
                    {

                    }

                }
                pageToken = files.NextPageToken;
            } while (pageToken != null);

            Console.Read();

        }

        private static void FileDownloadedCallback(Task obj, object state)
        {
            var stateInternal = (DownloadState)state;
            googleDriveSevicePool.Enqueue(stateInternal.DriveService);            
            RunningThreadCount--;
            if (obj.Status == TaskStatus.RanToCompletion)
            {
                SucsefullFileProcessedCount++;
            }
        }

        private static async Task DownloadFile(Google.Apis.Drive.v3.Data.File file, string newFilename, DriveService service)
        {
            var request = service.Files.Get(file.Id);
            using (var stream = new MemoryStream())
            using (var filestream = new FileStream(newFilename, FileMode.Create, FileAccess.Write))
            {
                await request.DownloadAsync(stream);
                stream.Position = 0;
                await stream.CopyToAsync(filestream);
                filestream.Flush();
            }
        }

    }


    public class DownloadState
    {
        public DriveService DriveService { get; set; }
        public Task Task { get; set; }
    }
}


//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace TurboCopy
//{
//    class Program
//    {
//        static string sourceFolder = @"Z:\facedb";
//        static string destinationFolder = @"C:\Users\Nikolas\source\repos\Faces\facedb";
//        static int numOfThreads = 20;
//        static int totalFileProcessedCount = 0;
//        static int SucsefullFileProcessedCount=0;
//        static int RunningThreadCount = 0;

//        static async Task Main(string[] args)
//        {
//            foreach(var file in Directory.EnumerateFiles(sourceFolder,"*.jpg",SearchOption.AllDirectories))
//            {

//                var newFilename = file.Replace(sourceFolder, destinationFolder);
//                if(!File.Exists(newFilename))
//                {
//                    while(RunningThreadCount>numOfThreads)
//                    {
//                        await Task.Delay(TimeSpan.FromTicks(1000));
//                    }

//                    CopyFileAsync(file, newFilename).ContinueWith(FileCopyFinishCallback);
//                    RunningThreadCount++;
//                    totalFileProcessedCount++;
//                    if (totalFileProcessedCount % 200 == 0)
//                    {
//                        Console.WriteLine($"Total: {totalFileProcessedCount} \t\t\t Sucsess:{SucsefullFileProcessedCount}");
//                    }

//                }

//            }

//        }

//        private static void FileCopyFinishCallback(Task result)
//        {
//            RunningThreadCount--;

//            if (result.Status==TaskStatus.RanToCompletion)
//            {
//                SucsefullFileProcessedCount++;
//            }
//        }

//        public static async Task CopyFileAsync(string sourcePath, string destinationPath)
//        {           
//            using (Stream source = File.OpenRead(sourcePath))
//            {
//                using (Stream destination = File.Create(destinationPath))
//                {
//                    await source.CopyToAsync(destination);
//                }
//            }
//        }


//    }
//}
