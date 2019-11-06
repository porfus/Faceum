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
        static int BatchSize = 1;
        static ConcurrentQueue<string[]> facePhotoToProcessQueue = new ConcurrentQueue<string[]>();
        static ConcurrentQueue<EmbeddingFaceModel> embeddingFaces = new ConcurrentQueue<EmbeddingFaceModel>();
        static int TotalFaceprocessing = 0;
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

            Metric.Config.WithReporting(x => x.WithReport(new ConsoleMetricReporter(null), TimeSpan.FromSeconds(20)));

            var files = Directory.EnumerateFiles(PathToFacePhotos, "*", SearchOption.AllDirectories);
            var batch = new List<string>();

            var threadFacePhotoProcessTask = new Thread(FacePhotoProcessTask);
            var threadSaveToDbTask = new Thread(SaveToDbTask);

            threadFacePhotoProcessTask.Start();
            threadSaveToDbTask.Start();

            foreach (var file in files)
            {
                batch.Add(file);
                if (batch.Count == BatchSize)
                {

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
                        if (embed != null)
                        {
                            for (var i = 0; i < embed.Count; i++)
                            {
                                var faceId = Path.GetFileNameWithoutExtension(batch[i]);
                                embeddingFaces.Enqueue(new EmbeddingFaceModel { FaceId = faceId, Embedding = embed[i] });
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
            while (true)
            {
                if (embeddingFaces.Count > 0)
                {
                    try
                    {
                        if (embeddingFaces.TryDequeue(out EmbeddingFaceModel data))
                        {
                            _collectionEmbedding.InsertOne(data);
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
