using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class WeatherForecastAPIWorker
    {
        private const string api_key = "5366cbefcc87eb47b5007c54e7ab73e7";
        private const string get_current_weather = "http://api.openweathermap.org/data/2.5/weather?q=CURRENT_CITY&units=metric";

        private const string get_weather_icon = " http://openweathermap.org/img/wn/ICON_ID@2x.png";

        public WeatherForecastAPIWorker()
        {

        }


        public string GetWeatherInCity(string cityName)
        {
            string request = String.Concat(get_current_weather.Replace("CURRENT_CITY", cityName), "&appid=", api_key);

            JObject o = JObject.Parse(HTTPWorker.Get(request));
            var m = o.GetValue("message");
            if (m == null)
            {
                string mainDescription = o.GetValue("weather").ToArray()[0].ToObject<JObject>().GetValue("main").ToString();
                string iconName = o.GetValue("weather").ToArray()[0].ToObject<JObject>().GetValue("icon").ToString();

                var mainParams = o.GetValue("main").ToObject<JObject>();

                string realTemp = mainParams.GetValue("temp").ToString();
                string feelsLikeTemp = mainParams.GetValue("feels_like").ToString();
                string minInCity = mainParams.GetValue("temp_min").ToString();
                string maxInCity = mainParams.GetValue("temp_max").ToString();
                string pressure = mainParams.GetValue("pressure").ToString();
                string humidity = mainParams.GetValue("humidity").ToString();
                string windSpeed = o.GetValue("wind").ToObject<JObject>().GetValue("speed").ToString();

                return String.Concat($"The weather in {cityName}:\n",
                    $"It is {mainDescription}, \n",
                    $"the temperature is {realTemp} degrees Celsius.\nBut it feels like {feelsLikeTemp}.\n",
                    $"The minimum temperature in city is {minInCity}. \nand maximum is {maxInCity}. \n",
                    $"Current pressure is {pressure}. \n",
                    $"And humidity is {humidity}%.\n",
                    $"The speed of wind is about {windSpeed} m/s",
                    get_weather_icon.Replace("ICON_ID", iconName));
            }

            return "City not found. Are you sure it's spelled like this?";

        }
    }
}
