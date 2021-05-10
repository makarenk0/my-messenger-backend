using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Text.Json;

namespace VirtualAssistant
{
    public class NewYorkTimesAPIWorker : INewsLoader
    {
        private const string api_key = "lPrbpG3RTK9Yq0PCEpD0OqHIcgl5m8IT";
        private const string get_most_popular_articles = "https://api.nytimes.com/svc/mostpopular/v2/emailed/7.json";

        public NewYorkTimesAPIWorker()
        {

        }

        public string GetRandomPopularArticle()
        {
            JObject o = JObject.Parse(HTTPWorker.Get(FormGetRequest()));
            var resultsNum = o.GetValue("num_results");
           

            Random rd = new Random();
            int num = rd.Next(0, Int32.Parse(resultsNum.ToString()));

            JObject article = o.GetValue("results").ToArray()[num].ToObject<JObject>();

            string title = article.GetValue("title").ToString();
            string abstractDesc = article.GetValue("abstract").ToString();
            var media = article.GetValue("media");
            string imgUrl = "";
            if (media != null)
            {
                try
                {
                    imgUrl = media.ToArray()[0].ToObject<JObject>().GetValue("media-metadata").ToArray()[2].ToObject<JObject>().GetValue("url").ToString();
                }
                catch(Exception e)
                {}
            }
            string articleUrl = article.GetValue("url").ToString();

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

        private string FormGetRequest()
        {
            return String.Concat(get_most_popular_articles, "?api-key=", api_key);
        }

        
    }
}
