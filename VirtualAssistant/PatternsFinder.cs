using System;
using System.Collections.Generic;
using System.Linq;
using Porter2Stemmer;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace VirtualAssistant
{
    public class PatternsFinder
    {
        EnglishPorter2Stemmer stemmer;

        Dictionary<string, HashSet<string>> _wordsTypes;
        Dictionary<string, Dictionary<string, string>> _patterns;

        Dictionary<string, LinkedList<string>> _cutWords;
        LinkedList<string> _keyWords;


        public PatternsFinder(string patternsFileNameJSON)
        {
            stemmer = new EnglishPorter2Stemmer();
            _wordsTypes = new Dictionary<string, HashSet<string>>();
            _patterns = new Dictionary<string, Dictionary<string, string>>();
            _cutWords = new Dictionary<string, LinkedList<string>>();
            _keyWords = new LinkedList<string>();

            LoadWordsStructures(patternsFileNameJSON);
        }

        private void LoadWordsStructures(string patternsFileNameJSON)
        {
            using (StreamReader file = File.OpenText(patternsFileNameJSON))
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
                        _patterns.Add(propertyName, new Dictionary<string, string>());
                        while (true)
                        {
                            reader.Read();
                            tokenType = reader.TokenType.ToString();
                            if (tokenType == "EndObject") break;

                            string key = reader.Value.ToString();
                            reader.Read();
                            string value = reader.Value.ToString();

                            _patterns[propertyName].Add(key, value);
                        }
                    }
                    else if (tokenType == "StartArray") //starting new words type reading
                    {
                        _wordsTypes.Add(propertyName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        while (true)
                        {
                            reader.Read();
                            tokenType = reader.TokenType.ToString();
                            if (tokenType == "EndArray") break;

                            _wordsTypes[propertyName].Add(reader.Value.ToString());
                        }
                    }
                }
            }
        }

        public string GetPatternAnswer(string input)
        {
            _cutWords.Clear();

            //First easiest try
            var answerG = CheckForGeneralQuestions(CutPatternWords(input.Split(' '), "Pronouns"));
            if (answerG.Key) return answerG.Value;
            _cutWords.Clear();

            //Second try
            var answerP = CheckForPatternStatements(input);
            if (answerP.Key) return answerP.Value;

            //Third try(cant recognize pattrens)

            return ChooseRandomly("Sorry, I dont understand you|Can you tell me this in other words?|Can you describe more accurately?");
        }

        private string[] GetSpecialCaseWords(string input)
        {
            input = input.Replace("(", "");
            input = input.Replace(")", "");
            string[] vs = input.Split('|'); ;
            return vs;
        }

        private KeyValuePair<bool, string> CheckForPatternStatements(string input)
        {
            input = input.ToLower();
            string castedInput = CastInputToGeneralType(input);

            string[] bareInputArr = DeleteIgnoreList(input).Split(' ');
            string[] casteInputArr = castedInput.Split(' ');

            foreach (KeyValuePair<string, string> pattern in _patterns["Statements"])
            {
                string[] wordsPatterns = pattern.Key.Split(' ');
                int patternWordsNum = wordsPatterns.Count(x => !x.Contains("("));
                bool fits = casteInputArr.Length >= patternWordsNum ? true : false;
                int j = 0;
                for (int i = 0; i < wordsPatterns.Length && j < bareInputArr.Length && fits; i++)
                {
                    if ((wordsPatterns[i].StartsWith("(")
                       && GetSpecialCaseWords(wordsPatterns[i]).Contains(bareInputArr[j])))
                    {
                        ++j;
                        continue;
                    }
                    else if (wordsPatterns[i] != casteInputArr[j])
                    {
                        fits = false;
                        break;
                    }
                    if (i < wordsPatterns.Length - 1 && !wordsPatterns[i + 1].StartsWith("("))
                    {
                        ++j;
                    }

                }
                if (fits)
                {
                    string patternAnswer = ChooseRandomly(pattern.Value);
                    string[] words = patternAnswer.Split(' ');
                    string output = "";
                    bool toReplaceNext = false;
                    foreach (string word in words)
                    {
                        if (word.StartsWith(">"))
                        {
                            if (word == ">r") toReplaceNext = true;
                            else
                            {
                                output = String.Concat(output, toReplaceNext ? GetReplacement(_cutWords[word].First.Value) : _cutWords[word].First.Value, " ");
                                _cutWords[word].RemoveFirst();
                                toReplaceNext = false;
                            }
                        }
                        else if (word == "*")
                        {
                            output = String.Concat(output, _keyWords.First.Value, " ");
                            _keyWords.RemoveFirst();
                        }
                        else
                        {
                            output = String.Concat(output, word, " ");
                        }

                    }
                    return new KeyValuePair<bool, string>(true, output.Trim());
                }
            }
            return new KeyValuePair<bool, string>(false, input);
        }

        private KeyValuePair<bool, string> CheckForGeneralQuestions(string input)
        {
            string[] words = input.Split(' ');
            if (words.Length > 1)
            {
                string twoWords = words[0] + ' ' + words[1];
                if (_wordsTypes["GeneralQuestions"].Contains(twoWords))
                {
                    return new KeyValuePair<bool, string>(true, GenerateGeneralAnswer(words[0], _cutWords[">p"].First.Value));
                }
            }
            return new KeyValuePair<bool, string>(false, input);
        }

        private string GenerateGeneralAnswer(string verb, string pronoun)
        {
            string output = "";
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[5];
                rg.GetBytes(rno);

                if (Math.Abs(BitConverter.ToInt32(rno, 0)) % 2 == 0) output += "No";
                else output += "Yes";

                rg.GetBytes(rno);
                if (Math.Abs(BitConverter.ToInt32(rno, 0)) % 2 == 0)
                {
                    if (output == "No") output = String.Concat("No, ", pronoun.ToLower(), " ", verb.ToLower(), " not");
                    else output = String.Concat("Yes, ", pronoun.ToLower(), " ", verb.ToLower());
                }
            }

            string changedVerbProunoun = output;

            changedVerbProunoun = output.Replace("i am", "you are");
            changedVerbProunoun = changedVerbProunoun.Replace("i ", "you ");

            if (changedVerbProunoun == output)
            {
                changedVerbProunoun = output.Replace("you are", "i am");
                changedVerbProunoun = changedVerbProunoun.Replace("you", "i");
            }

            return changedVerbProunoun;
        }

        private string CastInputToGeneralType(string input)
        {
            input = DeleteIgnoreList(input);
            input = CutPatternWords(input.Split(' '), "Pronouns");
            input = CutPatternWords(input.Split(' '), "Articles");
            input = CutPatternWords(input.Split(' '), "Verbs");
            input = CutPatternWords(input.Split(' '), "SpecialQuestions");
            input = CutPatternWords(input.Split(' '));
            return input;
        }

        private string CutPatternWords(string[] words, string patternWordsType)
        {
            string output = "";
            string patternSign = String.Concat(">", patternWordsType.Substring(0, 1).ToLower());
            _cutWords.Add(patternSign, new LinkedList<string>());
            foreach (string word in words)
            {
                if (_wordsTypes[patternWordsType].Contains(patternSign == ">v" ? stemmer.Stem(word).Value : word))  // if verb stemming it
                {
                    output += patternSign + " ";
                    _cutWords[patternSign].AddLast(patternSign == ">v" ? stemmer.Stem(word).Value : word);
                }
                else output += word + " ";
            }
            return output.Trim();
        }

        private string CutPatternWords(string[] words) //cut all(and not patterns)
        {
            string output = "";
            foreach (string word in words)
            {
                if (!word.StartsWith(">"))
                {
                    output += "* ";
                    _keyWords.AddFirst(word);
                }
                else output += word + " ";
            }
            return output.Trim();
        }

        private string DeleteIgnoreList(string input)
        {
            foreach (string ignore in _wordsTypes["IgnoreList"])
            {
                input = input.Replace(ignore, "");
            }
            return input.Trim();
        }

        private string GetReplacement(string input)
        {
            return _patterns["Replacements"].ContainsKey(input) ? _patterns["Replacements"][input] : input;
        }

        private string ChooseRandomly(string input)
        {
            string[] outputs = input.Split('|');
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[5];
                rg.GetBytes(rno);

                int n = Math.Abs(BitConverter.ToInt32(rno, 0)) % outputs.Length;
                return outputs[n];
            }
        }
    }
}
