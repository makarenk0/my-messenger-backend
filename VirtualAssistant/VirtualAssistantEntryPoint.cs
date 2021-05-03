using DatabaseModule.Entities;
using MyMessengerBackend.DatabaseModule;
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

        private enum State { none, WaitingForCity_weather, WaitingForTime_reminder}

        private State _currentState;

        private NewYorkTimesAPIWorker _newYorkTimesAPIWorker;
        private WeatherForecastAPIWorker _weatherForecastAPIWorker;
        private RemindersWorker _reminderWorker;

        private string _reminderContent;

        public delegate void UpdateActionFromAssistant();

        public VirtualAssistantEntryPoint(string patternsFileNameJSON, UserController userController, UpdateActionFromAssistant trigger)
        {
            _patternsFileNameJSON = patternsFileNameJSON;
            _commandsHandler = new CommandsHandler(patternsFileNameJSON);
            _patternsFinder = new PatternsFinder(patternsFileNameJSON);

            _newYorkTimesAPIWorker = new NewYorkTimesAPIWorker();
            _weatherForecastAPIWorker = new WeatherForecastAPIWorker();
            _reminderWorker = new RemindersWorker(userController, trigger);

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
            return CheckForCommand(answer, input);
        }

        private string ProcessState(string input)
        {
            switch (_currentState)
            {
                case State.WaitingForCity_weather:
                    _currentState = State.none;
                    return _weatherForecastAPIWorker.GetWeatherInCity(input);
                case State.WaitingForTime_reminder:
                    _currentState = State.none;
                    var res = _reminderWorker.ProcessReminderTime(input, _reminderContent);
                    _reminderContent = "";
                    return res.Item1 ? "Done!" : res.Item2;
                default:
                    return "none";
            }
        }

        private string CheckForCommand(string answer, string input)
        {
            if(answer.ElementAt(0) != '/')
            {
                return answer;
            }
            string[] command = answer.TrimStart('/').Split(" ");
            switch (command[0])
            {
                case "popular_news":
                    return _newYorkTimesAPIWorker.GetRandomPopularArticle();
                case "weather":
                      _currentState = State.WaitingForCity_weather;
                      return "In which city are you?";
                case "reminder":
                    command = command.Skip(1).ToArray();
                    _reminderContent = String.Join(" ", input.Split(" ").Skip(command.Length));
                    _currentState = State.WaitingForTime_reminder;
                    return "What time do you want me to make a reminder?";
                default:
                    return "Unrecognized command, check patterns.json";
            }

        }
    }
}
