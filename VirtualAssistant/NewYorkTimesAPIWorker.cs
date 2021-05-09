using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace VirtualAssistant
{
    public class NewYorkTimesAPIWorker
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

            return String.Concat(title, ". \n", abstractDesc, " ", imgUrl, " ", articleUrl);
        }

        private string FormGetRequest()
        {
            return String.Concat(get_most_popular_articles, "?api-key=", api_key);
        }

        
    }
}
