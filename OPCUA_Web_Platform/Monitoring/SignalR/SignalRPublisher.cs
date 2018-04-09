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
        private HubConnection _hubConnection;
        private IHubProxy _hub;
        private static readonly Dictionary<string, IHubProxy> ClientsDict = new Dictionary<string, IHubProxy>();

        public SignalRPublisher(string SignalRURL)
        {

            if (ClientsDict.ContainsKey(SignalRURL))
            {
                _hub = ClientsDict[SignalRURL];
            }
            else
            {
                _hubConnection = new HubConnection(SignalRURL);
                _hub = _hubConnection.CreateHubProxy("myHub");
                _hubConnection.Start();
                ClientsDict.Add(SignalRURL, _hub);
            }
        }

        public void Publish(string topic, string message)
        {
            _hub.Invoke("Send", message, topic);
        }
    }
}
