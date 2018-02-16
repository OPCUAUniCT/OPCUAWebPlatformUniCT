using System;
namespace WebPlatform.Configurations.OptionsModels
{
    public class OPCUAServersOptions
    {
        public OPCUAServersOptions() {}

        public OPCUAServerUrl[] Urls { get; set; }
    }

    public class OPCUAServerUrl {

        public OPCUAServerUrl() {}

        public string Name { get; set; }
        public string Url { get; set; }
    }
}
