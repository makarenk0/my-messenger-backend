using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VirtualAssistant
{
    public class TODOWorker
    {
        private UserController _userController;
        public TODOWorker(UserController controller)
        {
            _userController = controller;
        }

        public void AddItemToList(string todoContent)
        {
            _userController.AddElementToTodoList(todoContent);
        }

        public string GetWholeTodoList()
        {
            return _userController.GetTodoList();
        }

        public string RemoveTodo(string content)
        {
            return _userController.FinishedElementTodoList(content) ? "To do completed!" : "No such to do in the list";
        }

        public string RemoveTodo(int num)
        {
            return _userController.FinishedElementTodoList(num) ? "To do completed!" : "No such to do in the list";
        }
    }
}
