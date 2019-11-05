using FacesVkTestLoader.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Threading;
using System.Net;

namespace FacesVkTestLoader
{
    public class Downloader
    {

        private int procesingUrl = 0;
        private int maxProcessingUrls = 30;
        private IMongoCollection<PeopleModel> _collection;
        private IMongoCollection<PeopleVkModel> _collectionSource;
        private int totalProcessed = 0;
        private DateTime oldTime = DateTime.Now;
        private ConcurrentQueue<PeopleModel> queue = new ConcurrentQueue<PeopleModel>();

        public Downloader()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 1000;
            ThreadPool.SetMaxThreads(100, 100);

            string connectionString = "mongodb://localhost:27017";
            var client = new MongoClient(connectionString);
            var _db = client.GetDatabase("vk");
            _collection = _db.GetCollection<PeopleModel>("peoples");
            _collectionSource = _db.GetCollection<PeopleVkModel>("vkPeopleList");


        }


        public async Task Run(bool fromFreinds=false)
        {
            var baseuriFormat = @"https://xn--24-6kchq2abwi5bc.xn--p1ai/-{0}.html";
            var sourceIds = new List<long>();

            if (fromFreinds)
            {
                _collection.Find(x => true).Limit(600000).ToList().Select(x => x.FreindIds.Select(z=>long.Parse(z)))
                .ToList().ForEach(sourceIds.AddRange);
                sourceIds = sourceIds.Distinct().ToList();
            }
            else
            {
                sourceIds = (await _collectionSource.Find(x => true).ToListAsync()).Select(x => x.Id).ToList();
            }
            foreach (var peopleToProcess in sourceIds)
            {
                var i = peopleToProcess;
                var filter = Builders<PeopleModel>.Filter.Eq(x => x.VkId, i);
                var idCount = await _collection.Find(filter).CountDocumentsAsync();
                if (idCount > 0) continue;

                try
                {
                    var uri = string.Format(baseuriFormat, i);
                    while (procesingUrl > maxProcessingUrls)
                    {
                        await Task.Delay(15);
                    }
                    procesingUrl++;
                    var client = CreateHttpClient();
                    var task = client.GetStringAsync(uri).ContinueWith(ProcessResult1, i);

                }
                catch
                {
                    procesingUrl--;
                }
            }
        }

        private void ProcessResult1(Task<string> taskResult, object i)
        {

            procesingUrl--;

            if (taskResult.Status == TaskStatus.RanToCompletion)
            {
                try
                {
                    ProcessResult(taskResult.Result, (long)i);
                }
                catch
                {

                }
            }
            else
            {

            }

        }

       

        private HttpClient CreateHttpClient()
        {
            //currentProxyIndex++;
            //if (currentProxyIndex >= privateProxus.Count)
            //{
            //    currentProxyIndex = 0;
            //}
            //var proxy = new WebProxy
            //{

            //    Address = new Uri($"http://{privateProxus[currentProxyIndex].Item1}:{privateProxus[currentProxyIndex].Item2}"),

            //    UseDefaultCredentials = false,

            //    // *** These creds are given to the proxy server, not the web server ***
            //    Credentials = new NetworkCredential(
            //        userName: "gora60_yandex_ru",
            //        password: "ZkbNh370")
            //};

            //// Now create a client handler which uses that proxy
            //var httpClientHandler = new HttpClientHandler
            //{
            //    Proxy = proxy,
            //};
            return new HttpClient();// httpClientHandler);
        }

        private void ProcessResult(string result, long i)
        {
            if (string.IsNullOrWhiteSpace(result)) return;

            var hap = new HtmlAgilityPack.HtmlDocument();
            hap.LoadHtml(result);
            var root = hap.DocumentNode;
            var peopleModel = new PeopleModel();
            peopleModel.UserName = root.SelectSingleNode(@"//h1[@class='h4 author-name']").InnerText;
            peopleModel.VkId = i;
            peopleModel.UserLogo = root.SelectSingleNode(@"//a[@class='author-thumb']/img[1]").Attributes["src"].Value;


            var peronalInfoFields = root.SelectNodes(@"//ul[@class='widget w-personal-info']/li");

            try
            {
                if (peronalInfoFields != null && peronalInfoFields.Count > 0)
                {
                    foreach (var piField in peronalInfoFields)
                    {
                        var txt = piField.SelectSingleNode(@"span[1]").InnerText;
                        if (txt.Contains("Проживает в:"))
                        {
                            peopleModel.UserCity = piField.SelectSingleNode(@"span[2]").InnerText;
                        }
                        if (txt.Contains("День рождения"))
                        {
                            peopleModel.Birthday = piField.SelectSingleNode(@"span[2]").InnerText;
                        }
                    }
                }
            }
            catch
            {

            }

            try
            {
                var photoNodes = root.SelectNodes(@"//div[@class='ui-block-content']/a");
                if (photoNodes != null && photoNodes.Count > 0)
                {
                    foreach (var pNode in photoNodes)
                    {
                        var imgUrl = pNode.Attributes["href"].Value;
                        peopleModel.Photos.Add(imgUrl);
                    }
                }
            }
            catch
            {

            }

            try
            {
                var freindsNodes = root.SelectNodes(@"//div[@class='ui-block-content text-center']/a");
                if (freindsNodes != null && freindsNodes.Count > 0)
                {
                    foreach (var fNode in freindsNodes)
                    {
                        var freindurl = fNode.Attributes["href"].Value;
                        var frId = Regex.Match(freindurl, @"\d{1,10}").Value;
                        if (!string.IsNullOrEmpty(frId))
                        {
                            peopleModel.FreindIds.Add(frId);
                        }
                    }
                }
            }
            catch
            {

            }
            if (totalProcessed++ % 100 == 0)
            {
                var delta = (int)(DateTime.Now - oldTime).TotalMilliseconds;
                oldTime = DateTime.Now;
                Console.WriteLine($"{i}\t{delta}\t{peopleModel.UserName} ");
            }


            queue.Enqueue(peopleModel);

            lock (queue)
            {
                if (queue.Count > 1000)
                {
                    var buffer = queue.ToList();
                    queue.Clear();
                    _collection.InsertManyAsync(buffer);
                }
            }


            
        }
    }

}
