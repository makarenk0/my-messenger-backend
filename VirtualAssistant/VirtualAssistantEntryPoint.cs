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

        private enum State { none, WaitingForCity_weather, WaitingForTime_reminder, WaitingForApiSelect_news, WaitingForRemoveAgreement_todo}

        private State _currentState;

        private NewYorkTimesAPIWorker _newYorkTimesAPIWorker;
        private WeatherForecastAPIWorker _weatherForecastAPIWorker;
        private RemindersWorker _reminderWorker;
        private TODOWorker _todosWorker;

        private string _reminderContent;

        private string _todoToRemove;

        public delegate void UpdateActionFromAssistant();

        private UserController _userController;

        private Dictionary<string, INewsLoader> _newsApis;

        public VirtualAssistantEntryPoint(string patternsFileNameJSON, UserController userController, UpdateActionFromAssistant trigger)
        {
            _patternsFileNameJSON = patternsFileNameJSON;
            _commandsHandler = new CommandsHandler(patternsFileNameJSON);
            _patternsFinder = new PatternsFinder(patternsFileNameJSON);

          

            _newsApis = new Dictionary<string, INewsLoader>();
            _newsApis.Add("New York Times", new NewYorkTimesAPIWorker());
            _newsApis.Add("Free News", new FreeNewsApiWorker());

            _weatherForecastAPIWorker = new WeatherForecastAPIWorker();
            _reminderWorker = new RemindersWorker(userController, trigger);
            _todosWorker = new TODOWorker(userController);
            _userController = userController;
            _currentState = State.none;
        }

        public string Process(string input)
        {
            //if (!input.Contains('.'))
            //{
            //    return ProcessOneSentence(input);
            //}

            string complexAnswer = "";
            var sentences = input.Split('.', '?', '!', ',', ';').Where(x => !String.IsNullOrWhiteSpace(x)).ToArray(); 
            foreach(var sentence in sentences)
            {
                if (!String.IsNullOrWhiteSpace(sentence))
                {
                    complexAnswer = String.Concat(complexAnswer, ProcessOneSentence(sentence), sentences.Length == 1 ? "": ". ");
                }
            }
            return complexAnswer;
        }

        private string ProcessOneSentence(string input)
        {
            if (input.ElementAt(0) == '/')
            {
                return _commandsHandler.HandleCommands(input.TrimStart('/'));
            }
            else if (_currentState != State.none)
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
                case State.WaitingForApiSelect_news:
                    _currentState = State.none;
                    return ChooseApiForNews(input);
                case State.WaitingForRemoveAgreement_todo:
                    _currentState = State.none;
                    if (input.ToLower() == "yes" || input.ToLower() == "ok")
                    {
                        return _todosWorker.RemoveTodo(String.Join(' ', _todoToRemove.Split(' ').Skip(1)));
                    }
                    else if(input.ToLower() == "no")
                    {
                        _todosWorker.AddItemToList(_todoToRemove);
                        return "New todo was added!";
                    }
                    return "Can't understand you";
                default:
                    return "none";
            }
        }

        private string ChooseApiForNews(string input)
        {
            if (_newsApis.ContainsKey(input))
            {
                _userController.SetNewsApiVariant(input);
                return "Done!";
            }
            return "Unknown news source. Please pick one from offered variants";
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
                    string sourceApi = _userController.GetNewsApiVariant();
                    if (sourceApi == null)
                    {
                        _currentState = State.WaitingForApiSelect_news;
                        return String.Concat("Choose news source. Variants: ", String.Join(", ", _newsApis.Keys), 
                            " Later you could change your news source typing: \"Change news source\" or \"News source\"");
                    }
                    return _newsApis[sourceApi].GetRandomPopularArticle();
                case "change_news_api":
                    _currentState = State.WaitingForApiSelect_news;
                    return String.Concat("Choose news source. Variants: ", String.Join(", ", _newsApis.Keys));
                case "weather":
                    _currentState = State.WaitingForCity_weather;
                    return "In which city are you?";
                case "reminder":
                    command = command.Skip(1).ToArray();
                    var reminderContent = String.Join(" ", input.Split(" ").Skip(command.Length));
                    var res = _reminderWorker.ProcessReminderTime(reminderContent, "");
                    if (res.Item1)
                    {
                        return "Done!";
                    }
                    else if(res.Item2 == "time")
                    {
                        _reminderContent = reminderContent;
                        _currentState = State.WaitingForTime_reminder;
                        return "What time do you want me to make a reminder?";
                    }
                    return res.Item2;
                case "todo":
                    var commandArgs = command.Skip(1).ToArray();
                    var splittedInput = input.Split(" ");
                    if (splittedInput.Length == 1)
                    {
                        return _todosWorker.GetWholeTodoList();
                    }
                    
                    string todoContent = String.Join(" ", splittedInput.Skip(commandArgs.Length));
                    _todosWorker.AddItemToList(todoContent);
                    return "To do list was updated!";
                case "todo_remove":
                    var commandArgsR = command.Skip(1).ToArray();
                    var splittedInputR = input.Split(" ");
                   
                    var content = splittedInputR.Where(x => x != commandArgsR[0]).Where(y => y != commandArgsR[1]);
                    bool isNum = int.TryParse(content.First(), out int result);

                    if (!isNum)
                    {
                        _todoToRemove = commandArgsR[1]+ " " + String.Join(' ', content);
                        _currentState = State.WaitingForRemoveAgreement_todo;
                        return $"Do you want to remove \"{_todoToRemove}\" from your todo list? Send \"yes\" or \"no\" if you want to add such task.";
                    }
                    return _todosWorker.RemoveTodo(result);
                    
                default:
                    return "Unrecognized command, check patterns.json";
            }

        }
    }
}
