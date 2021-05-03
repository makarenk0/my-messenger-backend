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

        private const string dateTimeRegex = "^(?:(?:31(\\/|-|\\.)(?:0?[13578]|1[02]))\\1|(?:(?:29|30)(\\/|-|\\.)(?:0?[13-9]|1[0-2])\\2))(?:(?:1[6-9]|[2-9]\\d)?\\d{2})$|^(?:29(\\/|-|\\.)0?2\\3(?:(?:(?:1[6-9]|[2-9]\\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\\d|2[0-8])(\\/|-|\\.)(?:(?:0?[1-9])|(?:1[0-2]))\\4(?:(?:1[6-9]|[2-9]\\d)?\\d{2})\\sat\\s\\d{1,2}:\\d{2}$";

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
            Message mes = new Message() { Id = ObjectId.GenerateNewId(), Sender = "Assistant", Body = $"Reminder: {rem.ReminderContent}" };
            _userController.SendMessageToChat(_userController.User.AssistantChatId, mes);
            _userController.RemoveReminder(rem);
            _triggerUser();
        }

        public (bool, string) ProcessReminderTime(string input, string reminderContent)
        {
            var parts = input.Split(" ");
            var dateNow = DateTime.Now;
            Reminder newRem = null;
            if (Regex.Match(input, "^in\\s\\d+\\s.+$").Success)
            {
                var endDate = dateNow.AddSeconds(getTimeRangeInSec(int.Parse(parts[1]), parts[2]));
                newRem = new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate };
            }
            else if(Regex.Match(input, "^[a-z]+\\sat\\s\\d{1,2}:\\d{2}").Success)
            {
                var timeHourMinute = parts[2].Split(":");
                DateTime endDate = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, int.Parse(timeHourMinute[0]), int.Parse(timeHourMinute[1]), 0, DateTimeKind.Utc);
                if(parts[0] == "tomorrow")
                {
                    endDate = endDate.AddDays(1);
                }
                newRem = new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate.ToUniversalTime()};   
            }
            else if(Regex.Match(input, dateTimeRegex).Success)
            {
                char divSymbol = parts[0].ElementAt(parts[0].Length - 5);
                var dateParts = parts[0].Split(divSymbol);
                var timeHourMinute = parts[2].Split(":");
                DateTime endDate = new DateTime(int.Parse(dateParts[2]), int.Parse(dateParts[1]), int.Parse(dateParts[0]), int.Parse(timeHourMinute[0]), int.Parse(timeHourMinute[1]), 0, DateTimeKind.Utc);
                newRem = new Reminder() { Id = ObjectId.GenerateNewId(), ReminderContent = reminderContent, ReminderDateTime = endDate.ToUniversalTime() };
            }
            else
            {
                return (false, "Can't understand you(");
            }

            if(newRem.ReminderDateTime < DateTime.Now)
            {
                return (false, "I am not a time machine, i can make you a reminder only in future.");
            }
            _userController.CreateReminder(newRem);
            var taskRef = Task.Delay(newRem.ReminderDateTime - DateTime.Now).ContinueWith(t => SendReminderInRealTime(newRem));
            _delayedReminders.Add(taskRef);
            return (true, "done");
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
