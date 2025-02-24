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
        // Singleton instance of UserSession
        private static UserSession _instance;

        // Public property to get the singleton instance
        public static UserSession Instance => _instance ??= new UserSession();

        // Property to hold the current user
        public User? CurrentUser { get; private set; }

        // Private constructor to prevent instantiation
        private UserSession() { }

        // Method to set the current user
        public void SetUser(User user)
        {
            CurrentUser = user;
        }

        // Method to check if a user is signed in
        public bool IsUserSignedIn()
        {
            return CurrentUser != null;
        }
    }

}
