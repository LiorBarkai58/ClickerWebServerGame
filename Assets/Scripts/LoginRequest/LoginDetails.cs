namespace DefaultNamespace
{
    public class LoginDetails
    {
        public string Username;
		public string Password;
        
        public LoginDetails(string username, string password)
        {
            Username = username;
            Password = password;
        }
        
        public LoginDetails()
        {
            
        }
    }
}