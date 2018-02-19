using System;
namespace WebPlatform.Auth
{
    public interface IAuth
    {
        bool AuthenticateWithCredentials(string username, string password);
    }
}
