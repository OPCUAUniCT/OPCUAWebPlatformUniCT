using System;
namespace WebPlatform.Configurations.OptionsModels
{
    public class JwtOptions
    {
        public JwtOptions()
        {
        }

        public string Issuer { get; set; }
        public string Audience { get; set; }
    }
}
