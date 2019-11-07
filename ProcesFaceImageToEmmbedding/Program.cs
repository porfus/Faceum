using Metrics;
using Models;
using MongoDB.Driver;
using MtcnnNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcesFaceImageToEmmbedding
{
    class Program
    {
        static string PathToFacePhotos = "/photos/";
        static int BatchSize = 32;
        static ConcurrentQueue<string[]> facePhotoToProcessQueue = new ConcurrentQueue<string[]>();
        static ConcurrentQueue<EmbeddingFaceModel> embeddingFaces = new ConcurrentQueue<EmbeddingFaceModel>();
        static int TotalFaceprocessing = 0;
        static int TotalEmbeddedingsSaveToDb = 0;
        static int FileSkippedCount = 0;
        private static IMongoCollection<EmbeddingFaceModel> _collectionEmbedding;

        static void Main(string[] args)
        {

            string connectionString = "mongodb://79.143.30.220:27088";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            _collectionEmbedding = _db.GetCollection<EmbeddingFaceModel>("embeddingFace");

            Metric.Gauge("FacePhotoToProcessQueue Count", () => { return facePhotoToProcessQueue.Count; }, Unit.Items);
            Metric.Gauge("EmbeddingFaces Count", () => { return embeddingFaces.Count; }, Unit.Items);
            Metric.Gauge("TotalFaceprocessing Count", () => { return TotalFaceprocessing; }, Unit.Items);
            Metric.Gauge("TotalEmbeddedingsSaveToDb Count", () => { return TotalEmbeddedingsSaveToDb; }, Unit.Items); 
            Metric.Gauge("FileSkippedCount Count", () => { return FileSkippedCount; }, Unit.Items);

            Metric.Config.WithReporting(x => x.WithReport(new ConsoleMetricReporter(null), TimeSpan.FromSeconds(60)));

            var files = Directory.EnumerateFiles(PathToFacePhotos, "*", SearchOption.AllDirectories);
            var batch = new List<string>();

            var threadFacePhotoProcessTask = new Thread(FacePhotoProcessTask);
            var threadSaveToDbTask = new Thread(SaveToDbTask);

            threadFacePhotoProcessTask.Start();
            threadSaveToDbTask.Start();

            Console.Write("Getting processing file from db... ");
            var cursor = _collectionEmbedding.Find(x => true).ToCursor();
            var processedFilesFromDb = new HashSet<string>(3000000);
            while(cursor.MoveNext())
            {
                foreach(var processedFileFromDb in cursor.Current)
                {
                    processedFilesFromDb.Add(processedFileFromDb.FaceId);
                }
            }
            Console.WriteLine("Ok");

            foreach (var file in files)
            {   
                
                if (processedFilesFromDb.Contains(file))
                {
                    FileSkippedCount++;
                    continue;
                }

                batch.Add(file);

                if (batch.Count == BatchSize)
                {
                    TotalFaceprocessing++;
                    facePhotoToProcessQueue.Enqueue(batch.ToArray());
                    batch.Clear();
                }
                if (facePhotoToProcessQueue.Count > 100)
                {
                    Thread.Sleep(1000);
                }

            }
        }


        private static void FacePhotoProcessTask()
        {
            var faceInferiance = new FaceInferiance.FaceInferiance();
            while (true)
            {
                if (facePhotoToProcessQueue.Count > 0)
                {
                    if (facePhotoToProcessQueue.TryDequeue(out string[] batch))
                    {
                        var embed = faceInferiance.GetFaceEmmbeddins(batch);
                        if (embed != null && embed.Count == batch.Length)
                        {
                            for (var i = 0; i < embed.Count; i++)
                            {
                                var faceId = Path.GetFileNameWithoutExtension(batch[i]);
                                embeddingFaces.Enqueue(new EmbeddingFaceModel { FaceId = faceId, Embedding = embed[i] });
                            }
                        }
                        else
                        {
                            Console.WriteLine("Batch processing error");
                            foreach (var imgFileName in batch)
                            {
                                var singleEmmbedded = faceInferiance.GetFaceEmmbeddins(imgFileName);
                                if (singleEmmbedded != null && singleEmmbedded.Count == 1)
                                {
                                    var faceId = Path.GetFileNameWithoutExtension(imgFileName);
                                    embeddingFaces.Enqueue(new EmbeddingFaceModel { FaceId = faceId, Embedding = singleEmmbedded[0] });
                                }
                                else
                                {
                                    Console.WriteLine("Error with single file processing: " + imgFileName);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }


        private static void SaveToDbTask()
        {
            var buffer = new List<EmbeddingFaceModel>();
            while (true)
            {
                if (embeddingFaces.Count > 0)
                {
                    try
                    {
                        if (embeddingFaces.TryDequeue(out EmbeddingFaceModel data))
                        {
                            buffer.Add(data);
                            if (buffer.Count > 100)
                            {
                                try
                                {
                                    _collectionEmbedding.InsertMany(buffer);
                                    TotalEmbeddedingsSaveToDb += buffer.Count;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }
                                buffer.Clear();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

    }
}
