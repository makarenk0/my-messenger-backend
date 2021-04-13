using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class VirtualAssistantEntryPoint
    {
        private string _patternsFileNameJSON;
        private CommandsHandler _commandsHandler;
        private PatternsFinder _patternsFinder;

        private enum State { none, WaitingForCity_weather}

        private State _currentState;

        private NewYorkTimesAPIWorker _newYorkTimesAPIWorker;
        private WeatherForecastAPIWorker _weatherForecastAPIWorker;

        public VirtualAssistantEntryPoint(string patternsFileNameJSON)
        {
            _patternsFileNameJSON = patternsFileNameJSON;
            _commandsHandler = new CommandsHandler(patternsFileNameJSON);
            _patternsFinder = new PatternsFinder(patternsFileNameJSON);

            _newYorkTimesAPIWorker = new NewYorkTimesAPIWorker();
            _weatherForecastAPIWorker = new WeatherForecastAPIWorker();
            _currentState = State.none;
        }

        public string Process(string input)
        {
            if(input.ElementAt(0) == '/')
            {
                return _commandsHandler.HandleCommands(input.TrimStart('/'));
            }
            else if(_currentState != State.none)
            {
                return ProcessState(input);
            }

            string answer = _patternsFinder.GetPatternAnswer(input);
            return CheckForCommand(answer);
        }

        private string ProcessState(string input)
        {
            switch (_currentState)
            {
                case State.WaitingForCity_weather:
                    _currentState = State.none;
                    return _weatherForecastAPIWorker.GetWeatherInCity(input);
                default:
                    return "none";
            }
        }

        private string CheckForCommand(string input)
        {
            if(input.ElementAt(0) != '/')
            {
                return input;
            }
            string command = input.TrimStart('/');
            switch (command)
            {
                case "popular_news":
                    return _newYorkTimesAPIWorker.GetRandomPopularArticle();
                case "weather":
                      _currentState = State.WaitingForCity_weather;
                      return "In which city are you?";
                default:
                    return "Unrecognized command, check patterns.json";
            }

        }
    }
}
