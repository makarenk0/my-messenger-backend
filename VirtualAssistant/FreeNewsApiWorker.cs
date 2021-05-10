using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    class FreeNewsApiWorker : INewsLoader
    {
        public FreeNewsApiWorker()
        {

        }

        public string GetRandomPopularArticle()
        {

            JObject o = JObject.Parse(HTTPWorker.Get(FromRequest()));
            var resultsNum = o.GetValue("page_size");


            Random rd = new Random();
            int num = rd.Next(0, Int32.Parse(resultsNum.ToString()));

            JObject article = o.GetValue("articles").ToArray()[num].ToObject<JObject>();

            string title = article.GetValue("title").ToString();
            string abstractDesc = article.GetValue("summary").ToString();
            var media = article.GetValue("media");
            string imgUrl = "";
            if (media != null)
            {
                try
                {
                    imgUrl = media.ToString();
                }
                catch (Exception e)
                { }
            }
            string articleUrl = article.GetValue("link").ToString();


            OGTag oGTag = new OGTag()
            {
                Title = title,
                Description = abstractDesc,
                Image = imgUrl,
                Url = articleUrl
            };
            // Old version
            //String.Concat(title, ". \n", abstractDesc, " ", imgUrl, " ", articleUrl)

            return JsonSerializer.Serialize(oGTag);
        }

        private HttpRequestMessage FromRequest()
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://free-news.p.rapidapi.com/v1/search?q=news||popular||interesting&lang=en"),
                Headers =
                {
                    { "x-rapidapi-key", "9fd5cb8dd8msh901b91e67a37cb9p1fdb39jsn216a4022da7b" },
                    { "x-rapidapi-host", "free-news.p.rapidapi.com" },
                },
            };
            return request;
        }
    }
}
