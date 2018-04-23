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
        public string SecurityKey { get; set; }
        public int DurationMinutes { get; set; }
        public int RefreshTime { get; set; }
    }
}
