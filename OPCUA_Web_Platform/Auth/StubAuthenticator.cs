using System;
namespace WebPlatform.Auth
{
    public class StubAuthenticator: IAuth
    {
        public StubAuthenticator()
        {
        }

        public bool AuthenticateWithCredentials(string username, string password)
        {
            if (string.Equals(username, "Admin", StringComparison.OrdinalIgnoreCase) && password.Equals("password") )
            {
                return true;
            }

            if (string.Equals(username, "User1", StringComparison.OrdinalIgnoreCase) && password.Equals("password"))
            {
                return true;
            }

            return false;
        }
    }
}
