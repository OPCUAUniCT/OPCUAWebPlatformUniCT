using System;
namespace WebPlatform.Models.OptionsModels
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
