using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class CommandsHandler
    {
        private string _patternsFileNameJSON;
        private static string _helpInfo = "Use simple structures of sentences to interact with bot\nYou can ask or tell him something";
        private static string _keyForStorage = "1111";


        public CommandsHandler(string patternsFileNameJSON)
        {
            _patternsFileNameJSON = patternsFileNameJSON;

        }

        public string HandleCommands(string command)
        {
            string[] words = command.Split(' ');
            switch (words[0])
            {
                case "start":
                    return "Start your conversation";
                case "help":
                    return _helpInfo;
                case "info":
                    return GetBotInfo();
                case "add":
                    if (words.Length == 4) return words[1] == _keyForStorage ? AddNewWords(words) : "Invalid storage key!";
                    else return String.Concat("Please enter 4 parametrs: /add <storage key> <words type> <word to add>\n", GetBotInfo());
                case "metric":
                    if (words.Length == 1) return "Please enter 1 parametr: /metric <metric type>\nParametrs: cpu_percentage, cpu_average, memory_available," +
                             "requests_failed, requests_failed, requests_queue, requests_count";
                    switch (words[1])
                    {
                        case "cpu_percentage":
                            return "" + " %";
                        case "cpu_average":
                            return "" + " %";
                        case "memory_available":
                            return "" + " bytes";
                        case "requests_failed":
                            return "";
                        case "requests_queue":
                            return "";
                        case "requests_count":
                            return "";
                    }
                    return "No such metric!";
                default:
                    return "Command not found!";
            }
        }

        private string AddNewWords(string[] words)
        {
            using (StreamWriter file = new StreamWriter(_patternsFileNameJSON))
            using (JsonTextWriter reader = new JsonTextWriter(file))
            {

            }
            return "Is developing now";
        }

        private string GetBotInfo()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            string output = "I know:\n";

            using (StreamReader file = File.OpenText(_patternsFileNameJSON))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                reader.Read();
                string propertyName = "";
                while (reader.Read())
                {
                    string tokenType = reader.TokenType.ToString();
                    if (tokenType == "PropertyName")
                    {
                        propertyName = reader.Value.ToString();
                    }
                    else if (tokenType == "StartObject") //starting new pattern type reading
                    {
                        counts.Add(propertyName, 0);
                        while (true)
                        {
                            reader.Read();
                            tokenType = reader.TokenType.ToString();
                            if (tokenType == "EndObject") break;
                            reader.Read();
                            ++counts[propertyName];
                        }
                    }
                    else if (tokenType == "StartArray") //starting new words type reading
                    {
                        counts.Add(propertyName, 0);
                        while (true)
                        {
                            reader.Read();
                            tokenType = reader.TokenType.ToString();
                            if (tokenType == "EndArray") break;
                            ++counts[propertyName];
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, int> keyValue in counts)
            {
                output = String.Concat(output, keyValue.Value, " ", keyValue.Key, " ,\n");
            }
            return output;
        }
    }
}
