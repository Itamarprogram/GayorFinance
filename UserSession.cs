using model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GayorFinance
{
    public class UserSession
    {
        private static UserSession _instance;
        public static UserSession Instance => _instance ??= new UserSession();

        public User? CurrentUser { get; private set; }

        private UserSession() { }

        public void SetUser(User user)
        {
            CurrentUser = user;
        }

        public bool IsUserSignedIn()
        {
            return CurrentUser != null;
        }
    }

}
