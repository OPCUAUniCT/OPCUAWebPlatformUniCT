using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WebPlatform.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using NJsonSchema;
using Opc.Ua;
using Opc.Ua.Client;
using WebPlatform.Models.DataSet;
using WebPlatform.Models.OptionsModels;
using WebPlatform.Models.OPCUA;
using WebPlatform.OPCUALayer;
using WebPlatform.Exceptions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private readonly OPCUAServers[] _uaServers;
        private readonly IUaClientSingleton _uaClient;

        public ApiController(IOptions<OPCUAServersOptions> servers, IUaClientSingleton UAClient)
        {
            this._uaServers = servers.Value.Servers;
            for (int i = 0; i < _uaServers.Length; i++) _uaServers[i].Id = i;

            this._uaClient = UAClient;
        }

        [HttpGet("data-sets")]
        public IActionResult GetDataSets()
        {
            return Ok( _uaServers );
        }

        [HttpGet("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-(?:(\\d+)|(.+))$)?}")]
        public async Task<IActionResult> GetNode(int ds_id, string node_id = "0-85")
        {
            if (ds_id < 0 || ds_id >= _uaServers.Length) return NotFound($"There is no Data Set for id {ds_id}");
            
            var serverUrl = _uaServers[ds_id].Url;
            if (!(await _uaClient.IsServerAvailable(serverUrl)))
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");

            var decodedNodeId = WebUtility.UrlDecode(node_id);

            var result = new JObject();

            try 
            {
			    var sourceNode = await _uaClient.ReadNodeAsync(serverUrl, decodedNodeId);
                result["node-id"] = decodedNodeId;
                result["name"] = sourceNode.DisplayName.Text;

                switch (sourceNode.NodeClass)
                {
                    case NodeClass.Method:
                        result["type"] = "method";
                        break;
                    case NodeClass.Variable:
                        result["type"] = "variable";
                        var varNode = (VariableNode)sourceNode;
                        var uaValue = await _uaClient.ReadUaValueAsync(serverUrl, varNode);
                        result["value"] = uaValue.Value;
                        result["value-schema"] = JObject.Parse(uaValue.Schema.ToString());
                        result["status"] = uaValue.StatusCode?.ToString() ?? "";
                        result["deadBand"] = await _uaClient.GetDeadBandAsync(serverUrl, varNode);
                        result["minimumSamplingInterval"] = varNode.MinimumSamplingInterval;
                        break;
                    case NodeClass.Object:
                        result["type"] = await _uaClient.IsFolderTypeAsync(serverUrl, decodedNodeId) ? "folder" : "object";
                        break;
                }

                var linkedNodes = new JArray();
                var refDescriptions = await _uaClient.BrowseAsync(serverUrl, decodedNodeId);
                foreach (var rd in refDescriptions)
                {
                    var refTypeNode = await _uaClient.ReadNodeAsync(serverUrl, rd.ReferenceTypeId);
                    var targetNode = new JObject
                    {
                        ["node-id"] = rd.PlatformNodeId,
                        ["name"] = rd.DisplayName
                    };


                    switch (rd.NodeClass)
                    {
                        case NodeClass.Variable:
                            targetNode["Type"] = "variable";
                            break;
                        case NodeClass.Method:
                            targetNode["Type"] = "method";
                            break;
                        case NodeClass.Object:
                            targetNode["Type"] = await _uaClient.IsFolderTypeAsync(serverUrl, rd.PlatformNodeId)
                                ? "folder"
                                : "object";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    targetNode["relationship"] = refTypeNode.DisplayName.Text;

                    linkedNodes.Add(targetNode);
                }

                result["edges"] = linkedNodes;
            }
            catch (ServiceResultException exc)
            {
                switch (exc.StatusCode)
                {
                    case StatusCodes.BadNodeIdUnknown:
                        return NotFound(new
                        {
                            error = "Wrong ID: There is no Resource with ID " + decodedNodeId
                        });
                    case StatusCodes.BadNodeIdInvalid:
                        return BadRequest(new
                        {
                            error = "Provided ID is invalid"
                        });
                    case StatusCodes.BadSessionIdInvalid:
                    case StatusCodes.BadSessionClosed:
                    case StatusCodes.BadSessionNotActivated:
                    case StatusCodes.BadTooManySessions:
                        return StatusCode(500, new
                        {
                            error = "Connection Lost"
                        });
                    default:
                        return StatusCode(500, new
                        {
                            error = exc.Message
                        });
                }
            }
            catch (DataSetNotAvailableException)
            {
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");
            }
            
            return Ok(result);
        }

        [HttpPost("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-(?:(\\d+)|(.+))$)?}")]
        public async Task<IActionResult> PostNodeAsync(int ds_id, string node_id, [FromBody] VariableState state)
        {
            if (state == null || !state.IsValid)
                return BadRequest(new
                {
                    error = "Insert a valid state for a Variable Node."
                });

            if (ds_id < 0 || ds_id >= _uaServers.Length) return NotFound($"There is no Data Set for id {ds_id}");

            var serverUrl = _uaServers[ds_id].Url;
            if (!(await _uaClient.IsServerAvailable(serverUrl)))
                return StatusCode(500, new
                {
                    error = "Data Set " + ds_id + " NotAvailable"
                });

            var decodedNodeId = WebUtility.UrlDecode(node_id);

            Node sourceNode;
            try
            {
                sourceNode = await _uaClient.ReadNodeAsync(serverUrl, decodedNodeId);
            }
            catch (ServiceResultException exc)
            {
                switch (exc.StatusCode)
                {
                    case StatusCodes.BadNodeIdUnknown:
                        return NotFound(new
                        {
                            error = "Wrong ID: There is no Resource with ID " + decodedNodeId
                        });
                    case StatusCodes.BadNodeIdInvalid:
                        return BadRequest(new
                        {
                            error = "Provided ID is invalid"
                        });
                    case StatusCodes.BadSessionIdInvalid:
                    case StatusCodes.BadSessionClosed:
                    case StatusCodes.BadSessionNotActivated:
                    case StatusCodes.BadTooManySessions:
                        return StatusCode(500, new
                        {
                            error = "Connection Lost"
                        });
                    default:
                        return StatusCode(500, new
                        {
                            error = exc.Message
                        });
                }
            }
            catch (DataSetNotAvailableException)
            {
                return StatusCode(500, new
                {
                    error = "Data Set " + ds_id + " NotAvailable"
                });
            }

            if (sourceNode.NodeClass != NodeClass.Variable)
                return BadRequest(new
                {
                    error = "There is no Value for the Node specified by the NodeId " + node_id
                });

            VariableNode variableNode = (VariableNode)sourceNode;
            
            try
            {
                await _uaClient.WriteNodeValueAsync(serverUrl, variableNode, state);
            }
            catch(ValueToWriteTypeException exc)
            {
                return BadRequest(new
                {
                    error = exc.Message
                });
            }
            catch(NotImplementedException exc)
            {
                return StatusCode(500, new
                {
                    error = exc.Message
                });
            }
            catch(ServiceResultException exc)
            {
                switch (exc.StatusCode)
                {
                    case (StatusCodes.BadTypeMismatch): return BadRequest(new
                    {
                        error = "Wrong Type - Check data and try again"
                    });
                    case StatusCodes.BadSessionIdInvalid:
                    case StatusCodes.BadSessionClosed:
                    case StatusCodes.BadSessionNotActivated:
                    case StatusCodes.BadTooManySessions:
                        return StatusCode(500, new
                        {
                            error = "Connection Lost"
                        });
                    default: return BadRequest(new
                    {
                        error = exc.Message
                    });
                }
                    
            }
            return Ok("Write on Node {node_id} in the Data Set {ds_id} executed.");
        }

        [HttpPost("data-sets/{ds_id:int}/monitor")]
        public async Task<IActionResult> Monitor(int ds_id, [FromBody] MonitorParams monitorParams)
        {
            if (ds_id < 0 || ds_id >= _uaServers.Length) return NotFound($"There is no Data Set for id {ds_id}");

            var serverUrl = _uaServers[ds_id].Url;
            if (!(await _uaClient.IsServerAvailable(serverUrl)))
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");
            
            if (monitorParams == null || !monitorParams.IsValid())
            {
                return BadRequest(new
                {
                    error = "Bad parameters format."
                });
            }

            if (!monitorParams.IsTelemetryProtocolSupported())
            {
                return BadRequest(new
                {
                    error = "Telemetry protocol provided in the broker url is not supported by the platform."
                });
            }

            foreach (var monitorableNode in monitorParams.MonitorableNodes)
            {
                if (!new List<string> {"Absolute", "Percent", "None"}.Contains(monitorableNode.DeadBand))
                {
                    return BadRequest(new
                    {
                        error = $"Value not allowed for DeadBand parameter. Found '{monitorableNode.DeadBand}'"
                    });
                }
            }
            
            bool[] results;
            try 
            {
                results = await _uaClient.CreateMonitoredItemsAsync(serverUrl, 
                    monitorParams.MonitorableNodes, 
                    monitorParams.BrokerUrl, 
                    monitorParams.Topic);
            }
            catch (ServiceResultException exc)
            {
                switch (exc.StatusCode)
                {
                    case StatusCodes.BadNodeIdUnknown:
                        return NotFound("There is no node with the specified Node Id");
                    case StatusCodes.BadNodeIdInvalid:
                        return BadRequest("Provided Node Id is invalid");
                    case StatusCodes.BadSessionIdInvalid:
                    case StatusCodes.BadSessionClosed:
                    case StatusCodes.BadSessionNotActivated:
                    case StatusCodes.BadTooManySessions:
                        return StatusCode(500, new
                        {
                            error = "Connection Lost"
                        });
                    default:
                        return StatusCode(500, exc.Message);
                }
            }
            catch(DataSetNotAvailableException)
            {
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");
            }
            
            
            return Ok(new
            {
                results
            });
        }

        [HttpPost("data-sets/{ds_id:int}/stop-monitor")]
        public async Task<IActionResult> StopMonitor(int ds_id, [FromBody] StopMonitorParams stopMonitorParams)
        {
            if (ds_id < 0 || ds_id >= _uaServers.Length) return NotFound($"There is no Data Set for id {ds_id}");
            
            if (stopMonitorParams == null || !stopMonitorParams.IsValid())
            {
                return BadRequest(new
                {
                    error = "Bad parameters format."
                });
            }
            
            var serverUrl = _uaServers[ds_id].Url;
            var result = await _uaClient.DeleteMonitoringPublish(serverUrl, stopMonitorParams.BrokerUrl,
                    stopMonitorParams.Topic);

            if (result)
            {
                return Ok($"Successfully stop monitoring  on broker {stopMonitorParams.BrokerUrl}.");
            }
            
            return BadRequest(new
            {
                error = $"An error occurred trying to delete the topic {stopMonitorParams.Topic} on broker {stopMonitorParams.BrokerUrl}. " +
                        $"Maybe there is no current monitoring for such parameters or an internal error occurred in the Data Set."
            });
        }

    }
}
