using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class HTTPWorker
    {

        public static string Get(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch(WebException e)
            {
                JObject errorObj = new JObject();
                errorObj.Add("message", e.Message);
                return errorObj.ToString();
            }
            
        }

        public static string Get(HttpRequestMessage uri)
        {
            var client = new HttpClient();
            using (var response = client.Send(uri))
            {
                response.EnsureSuccessStatusCode();
                var body = response.Content.ReadAsStringAsync();
                return body.Result;
            }
        }
    }
}
