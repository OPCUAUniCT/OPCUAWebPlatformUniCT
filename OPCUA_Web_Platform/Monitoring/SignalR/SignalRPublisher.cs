using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Monitoring.SignalR
{
    interface ISignalRPublisher : IPublisher { }
    public class SignalRPublisher: ISignalRPublisher
    {
        private readonly IHubProxy _hub;
        private static readonly Dictionary<string, IHubProxy> ClientsDict = new Dictionary<string, IHubProxy>();

        public SignalRPublisher(string signalRUrl)
        {
            lock (ClientsDict)
            {
                if (ClientsDict.ContainsKey(signalRUrl))
                {
                    _hub = ClientsDict[signalRUrl];
                }
                else
                {
                    var hubConnection = new HubConnection(signalRUrl);
                    _hub = hubConnection.CreateHubProxy("myHub");
                    hubConnection.Start();
                    ClientsDict.Add(signalRUrl, _hub);
                }
            }
        }

        public void Publish(string topic, string message)
        {
            _hub.Invoke("Send", message, topic);
        }
    }
}
