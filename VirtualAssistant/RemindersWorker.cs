using DatabaseModule.Entities;
using MongoDB.Bson;
using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static VirtualAssistant.VirtualAssistantEntryPoint;

namespace VirtualAssistant
{
    public class RemindersWorker
    {
        private UserController _userController;

        private UpdateActionFromAssistant _triggerUser;

        private List<Task> _delayedReminders;


        private const string timeInRegex = "in\\s\\d+\\s.+";
        private const string todayTommorowRegex = "[a-z]+\\sat\\s\\d{1,2}:\\d{2}";
        private const string dateTimeRegex = "(?:(?:31(\\/|-|\\.)(?:0?[13578]|1[02]))\\1|(?:(?:29|30)(\\/|-|\\.)(?:0?[13-9]|1[0-2])\\2))(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$|^(?:29(\\/|-|\\.)0?2\\3(?:(?:(?:1[6-9]|[2-9]\\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\\d|2[0-8])(\\/|-|\\.)(?:(?:0?[1-9])|(?:1[0-2]))\\4(?:(?:1[6-9]|[2-9]\\d)?\\d{2})\\sat\\s\\d{1,2}:\\d{2}";



        public RemindersWorker(UserController userController, UpdateActionFromAssistant trigger)
        {
            _userController = userController;
            _triggerUser = trigger;
            _delayedReminders = new List<Task>();
            CheckForExpiredReminders();
        }

        ~RemindersWorker()
        {
            foreach(var t in _delayedReminders)
            {
                t.Dispose();
            }
        }

        public void CheckForExpiredReminders()
        {
            var reminders = _userController.GetActiveReminders();
            foreach(var rem in reminders)
            {
                if(rem.ReminderDateTime < DateTime.Now)
                {
                    SendReminderInRealTime(rem);
                }
                else
                {
                    var taskRef = Task.Delay(rem.ReminderDateTime - DateTime.UtcNow).ContinueWith(t => SendReminderInRealTime(rem));
                    _delayedReminders.Add(taskRef);
                }
            }
        }

        private void SendReminderInRealTime(Reminder rem)
        {
            Message mes = new Message() { 
                Id = ObjectId.GenerateNewId(), 
                Sender = "Assistant", 
                Body = $"Reminder: {rem.ReminderContent}",
                DeletedForUsers = new List<ObjectId>()
            };
            _userController.SendMessageToChat(_userController.User.AssistantChatId, mes);
            _userController.RemoveReminder(rem);
            _triggerUser();
        }

        public (bool, string) ProcessReminderTime(string input, string reminderContent)
        {
            Reminder newRem = null;

            #region RegexCheck
            var timeInRegexRes = Regex.Match(input, $"{timeInRegex}$");
            var todayTommorowRegexRes = Regex.Match(input, $"{todayTommorowRegex}$");
            var dateTimeRegexRes = Regex.Match(input, $"{dateTimeRegex}$");
            #endregion

            #region ProcessDataIfCorrect
            if (timeInRegexRes.Success)
            {
                newRem = TimeInRegexProcess(DateTime.Now, timeInRegexRes.Value.Split(" "), 
                    String.IsNullOrWhiteSpace(reminderContent) ? input.Replace(timeInRegexRes.Value, "") : reminderContent);
            }
            else if(todayTommorowRegexRes.Success)
            {
                newRem = TodayTommorowRegexProcess(DateTime.Now, todayTommorowRegexRes.Value.Split(" "),
                    String.IsNullOrWhiteSpace(reminderContent) ? input.Replace(todayTommorowRegexRes.Value, "") : reminderContent);
            }
            else if(dateTimeRegexRes.Success)
            {
                newRem = DateTimeRegexProcess(dateTimeRegexRes.Value.Split(" "),
                   String.IsNullOrWhiteSpace(reminderContent) ? input.Replace(dateTimeRegexRes.Value, "") : reminderContent);
            }
            else if (String.IsNullOrWhiteSpace(reminderContent))
            {
                return (false, "time");
            }
            else
            {
                return (false, "Can't understand you(");
            }
            #endregion


            #region CreatingReminder
            if (newRem.ReminderDateTime < DateTime.Now)
            {
                return (false, "I am not a time machine, i can make you a reminder only in future.");
            }
            _userController.CreateReminder(newRem);
            var taskRef = Task.Delay(newRem.ReminderDateTime - DateTime.Now).ContinueWith(t => SendReminderInRealTime(newRem));
            _delayedReminders.Add(taskRef);
            return (true, "done");
            #endregion
        }

        private Reminder TimeInRegexProcess(DateTime current, string[] parts, string reminderContent)
        {
            var endDate = current.AddSeconds(getTimeRangeInSec(int.Parse(parts[1]), parts[2]));
            return new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate };
        }

        private Reminder TodayTommorowRegexProcess(DateTime current, string[] parts, string reminderContent)
        {
            var timeHourMinute = parts[2].Split(":");
            DateTime endDate = new DateTime(current.Year, current.Month, current.Day, int.Parse(timeHourMinute[0]), int.Parse(timeHourMinute[1]), 0, DateTimeKind.Utc);
            if (parts[0] == "tomorrow")
            {
                endDate = endDate.AddDays(1);
            }
            return new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate.ToUniversalTime() };
        }

        private Reminder DateTimeRegexProcess(string[] parts, string reminderContent)
        {
            char divSymbol = parts[0].ElementAt(parts[0].Length - 5);
            var dateParts = parts[0].Split(divSymbol);
            var timeHourMinute = parts[2].Split(":");
            DateTime endDate = new DateTime(int.Parse(dateParts[2]), int.Parse(dateParts[1]), int.Parse(dateParts[0]), int.Parse(timeHourMinute[0]), int.Parse(timeHourMinute[1]), 0, DateTimeKind.Utc);
            return new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate.ToUniversalTime() };
        }




        private int getTimeRangeInSec(int amount, string input)
        {
            if (input.Contains("second"))
            {
                return amount;
            }
            else if (input.Contains("minute"))
            {
                return amount * 60;
            }
            else if (input.Contains("hour"))
            {
                return amount * 60 * 60;
            }
            else if (input.Contains("day"))
            {
                return amount * 60 * 60 * 24;
            }
            else if (input.Contains("week"))
            {
                return amount * 60 * 60 * 24 * 7;
            }
            return 0;
        }

        
    }
}
