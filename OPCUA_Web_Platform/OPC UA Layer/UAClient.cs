using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebPlatform.Extensions;
using Opc.Ua;
using Opc.Ua.Client;
using WebPlatform.Models.OPCUA;
using WebPlatform.Exceptions;

namespace WebPlatform.OPCUALayer
{
    public interface IUAClient
    {
        Task<Node> ReadNodeAsync(string serverUrl, string nodeIdStr);
        Task<Node> ReadNodeAsync(string serverUrl, NodeId nodeId);
        Task<ReferenceDescriptionCollection> BrowseAsync(string serverUrl, string nodeToBrowseIdStr);
        Task<UaValue> ReadUaValueAsync(string serverUrl, VariableNode varNode);
        Task<bool> IsFolderTypeAsync(string serverUrlstring, string nodeIdStr);
        Task<bool> isServerAvailable(string serverUrlstring);
    }

    public interface IUAClientSingleton : IUAClient { }

    public class UAClient : IUAClientSingleton
    {
        private ApplicationConfiguration _appConfiguration { get; }
        //A Dictionary containing al the activ Sessions, indexed per server Id.
        private Dictionary<string, Session> _sessions;

        private struct Endpoint
        {
            public int EndpointId { get; set; }
            public string EndpointUrl { get; set; }
            public string SecurityMode { get; set; }
            public string SecurityLevel { get; set; }
            public string SecurityPolicyUri { get; set; }


            public Endpoint(int id, string url, string securityMode, string securityLevel, string securityPolicyUri)
            {
                EndpointId = id;
                EndpointUrl = url;
                SecurityMode = securityMode;
                SecurityLevel = securityLevel;
                SecurityPolicyUri = securityPolicyUri;
            }
        }

        public UAClient()
        {
            this._appConfiguration = CreateAppConfiguration("OPCUAWebPlatform", 60000);
            this._sessions = new Dictionary<string, Session>();
        }

        public async Task<Node> ReadNodeAsync(string serverUrl, string nodeIdStr)
        {
            Session session = await GetSessionByUrlAsync(serverUrl);
            NodeId nodeToRead = ParsePlatformNodeIdString(nodeIdStr);
            Node node;
            try
            {
                node = session.ReadNode(nodeToRead);
            }
            catch (ServiceResultException)
            {
                throw new DataSetNotAvailableException();
            }
            return node;
        }

        public async Task<Node> ReadNodeAsync(string serverUrl, NodeId nodeToRead)
        {
            Session session = await GetSessionByUrlAsync(serverUrl);
            Node node;
            try {
                node = session.ReadNode(nodeToRead);
            }
            catch (ServiceResultException)
            {
                throw new DataSetNotAvailableException();
            }
            return node;
        }

        public async Task<ReferenceDescriptionCollection> BrowseAsync(string serverUrl, string nodeToBrowseIdStr)
        {
            Session session = await GetSessionByUrlAsync(serverUrl);
            NodeId nodeToBrowseId = ParsePlatformNodeIdString(nodeToBrowseIdStr);

            var browser = new Browser(session)
            {
                NodeClassMask = 0,
                ResultMask = (uint)BrowseResultMask.DisplayName | (uint)BrowseResultMask.NodeClass | (uint)BrowseResultMask.ReferenceTypeInfo,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences
            };

            return browser.Browse(nodeToBrowseId);
        }

        public async Task<bool> IsFolderTypeAsync(string serverUrl, string nodeIdStr)
        {
            Session session = await GetSessionByUrlAsync(serverUrl);
            NodeId nodeToBrowseId = ParsePlatformNodeIdString(nodeIdStr);

            //Set a Browser object to follow HasTypeDefinition Reference only
            var browser = new Browser(session)
            {
                ResultMask = (uint)BrowseResultMask.DisplayName | (uint)BrowseResultMask.TargetInfo,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasTypeDefinition
            };


            ReferenceDescription refDescription = browser.Browse(nodeToBrowseId)[0];
            //NodeId targetId = ExpandedNodeId.ToNodeId(refDescription.NodeId, null);
            NodeId targetId = refDescription.NodeId.ToNodeId();

            //Once got the Object Type, set the browser to follow Type hierarchy in inverse order.
            browser.ReferenceTypeId = ReferenceTypeIds.HasSubtype;
            browser.BrowseDirection = BrowseDirection.Inverse;

            while (targetId != ObjectTypeIds.FolderType && targetId != ObjectTypeIds.BaseObjectType)
            {
                refDescription = browser.Browse(targetId)[0];
                targetId = refDescription.NodeId.ToNodeId();
            }
            return targetId == ObjectTypeIds.FolderType;
        }

        public async Task<UaValue> ReadUaValueAsync(string serverUrl, VariableNode variableNode)
        {
            Session session = await GetSessionByUrlAsync(serverUrl);
            var typeManager = new DataTypeManager(session);
            //DataValue dataValue = ReadDataValue(session, variableNode.NodeId)[0];

            return typeManager.GetUaValue(variableNode);
        }

        public async Task<bool> isServerAvailable(string serverUrlstring)
        {
            Session session;
            if (!_sessions.ContainsKey(serverUrlstring))
            {
                var endpoints = new List<Endpoint>();
                var endpoint_id = 0;
                try
                {
                    foreach (EndpointDescription s in GetEndpointNames(new Uri(serverUrlstring)))
                    {
                        endpoints.Add(new Endpoint(endpoint_id, s.EndpointUrl, s.SecurityMode.ToString(), s.SecurityLevel.ToString(), s.SecurityPolicyUri));
                        endpoint_id++;
                    }
                }
                catch (Exception exc)
                {
                    return false;
                }
                //TODO: Prende sempre l'endpoint 0, verificare chi o cosa è.
                #warning Indice fisso a 0
                await CreateSessionAsync(serverUrlstring, endpoints[0].EndpointUrl, endpoints[0].SecurityMode, endpoints[0].SecurityPolicyUri);
            }

            session = _sessions[serverUrlstring];
            DataValue serverStatus;
            try
            {
                serverStatus = session.ReadValue(new NodeId(2259, 0));
            }
            catch (Exception exc)
            {
                return await RestoreSessionAsync(serverUrlstring);
            }
            //If StatusCode of the Variable read is not Good or if the Value is not equal to Running (0)
            //the OPC UA Server is not available
            if (DataValue.IsNotGood(serverStatus) || (int)serverStatus.Value != 0)
                return false;
            return true;
        }

        /*//TODO: sposta la funzione sotto tra le private
        private DataValueCollection ReadDataValue(Session session, NodeId nodeId)
        {
            ReadValueIdCollection nodeToRead 
        }*/

        #region private methods

            /// <summary>
            /// This method is called when a OPC UA Service call in a session object returns an error 
            /// </summary>
            /// <param name="serverUrlstring"></param>
            /// <returns></returns>
            private async Task<bool> RestoreSessionAsync(string serverUrlstring)
        {
            _sessions.Remove(serverUrlstring);
            var endpoints = new List<Endpoint>();
            var endpoint_id = 0;
            try
            {
                foreach (EndpointDescription s in GetEndpointNames(new Uri(serverUrlstring)))
                {
                    endpoints.Add(new Endpoint(endpoint_id, s.EndpointUrl, s.SecurityMode.ToString(), s.SecurityLevel.ToString(), s.SecurityPolicyUri));
                    endpoint_id++;
                }
                await CreateSessionAsync(serverUrlstring, endpoints[0].EndpointUrl, endpoints[0].SecurityMode, endpoints[0].SecurityPolicyUri);
            }
            catch (Exception exc)
            {
                return false;
            }
            return true;
        }

        private async Task<Session> GetSessionByUrlAsync(string url)
        {
            if (_sessions.ContainsKey(url))
                return _sessions[url];

            else
            {
                var endpoints = new List<Endpoint>();
                var endpoint_id = 0;
                try
                {
                    foreach (EndpointDescription s in GetEndpointNames(new Uri(url)))
                    {
                        endpoints.Add(new Endpoint(endpoint_id, s.EndpointUrl, s.SecurityMode.ToString(), s.SecurityLevel.ToString(), s.SecurityPolicyUri));
                        endpoint_id++;
                    }
                }
                catch (ServiceResultException exc)
                {
                    switch (exc.StatusCode)
                    {
                        case (StatusCodes.BadNotConnected):
                            throw new DataSetNotAvailableException();
                    }
                }
                //TODO: Prende sempre l'endpoint 0, verificare chi o cosa è.
                #warning Indice fisso a 0
                return await CreateSessionAsync(url, endpoints[0].EndpointUrl, endpoints[0].SecurityMode, endpoints[0].SecurityPolicyUri);

            }
        }

        private async Task<Session> CreateSessionAsync(string serverUrl, string endpointUrl, string securityMode, string securityPolicy)
        {
            await _appConfiguration.Validate(ApplicationType.Client);
            _appConfiguration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;

            var endpointDescription = new EndpointDescription(endpointUrl);
            endpointDescription.SecurityMode = (MessageSecurityMode)Enum.Parse(typeof(MessageSecurityMode), securityMode, true);
            endpointDescription.SecurityPolicyUri = securityPolicy;

            var endpointConfiguration = EndpointConfiguration.Create(_appConfiguration);

            var endpoint = new ConfiguredEndpoint(endpointDescription.Server, endpointConfiguration);
            endpoint.Update(endpointDescription);

            Console.WriteLine($"Creo la sessione con {endpointDescription.Server} \n\tSecurityMode: {endpointDescription.SecurityMode}\n\tSecurityPolicy: {endpointDescription.SecurityPolicyUri}");

            var s = await Session.Create(_appConfiguration,
                                             endpoint,
                                             true,
                                             false,
                                             _appConfiguration.ApplicationName + "_session",
                                             (uint)_appConfiguration.ClientConfiguration.DefaultSessionTimeout,
                                             null,
                                             null);

            _sessions.Add(serverUrl, s);

            return s;
        }

        private ApplicationConfiguration CreateAppConfiguration(string applicationName, int sessionTimeout)
        {
            var config = new ApplicationConfiguration()
            {
                ApplicationName = applicationName,
                ApplicationType = ApplicationType.Client,
                ApplicationUri = "urn:localhost:OPCFoundation:" + applicationName,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "./OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = Utils.Format("CN={0}, DC={1}", applicationName, Utils.GetHostName())
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "./OPC Foundation/CertificateStores/UA Applications",
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "./OPC Foundation/CertificateStores/UA Certificate Authorities",
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "./OPC Foundation/CertificateStores/RejectedCertificates",
                    },
                    NonceLength = 32,
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = sessionTimeout }
            };

            return config;
        }

        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }

        private EndpointDescriptionCollection GetEndpointNames(Uri serverURI)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(_appConfiguration);
            configuration.OperationTimeout = 10;

            using (DiscoveryClient client = DiscoveryClient.Create(serverURI, EndpointConfiguration.Create(_appConfiguration)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    return endpoints;
                }
                catch (ServiceResultException exc)
                {
                    throw exc;
                }
            }
        }

        private NodeId ParsePlatformNodeIdString(string str)
        {
            const string pattern = @"^(\d+)-(?:(\d+)|(\S+))$";
            var match = Regex.Match(str, pattern);
            var isString = match.Groups[3].Length != 0;
            var isNumeric = match.Groups[2].Length != 0;

            var idStr = (isString) ? $"s={match.Groups[3]}" : $"i={match.Groups[2]}";
            var builtStr = $"ns={match.Groups[1]};" + idStr;

            return new NodeId(builtStr);
        }


        #endregion

    }
}