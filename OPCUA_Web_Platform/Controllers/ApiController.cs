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
using System;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private OPCUAServers[] _UAServers;
        private IUaClientSingleton _UAClient;

        public ApiController(IOptions<OPCUAServersOptions> servers, IUaClientSingleton UAClient)
        {
            this._UAServers = servers.Value.Servers;
            for (int i = 0; i < _UAServers.Length; i++) _UAServers[i].Id = i;

            this._UAClient = UAClient;
        }

        [HttpGet("data-sets")]
        public IActionResult GetDataSets()
        {
            return Ok( _UAServers );
        }

        [HttpGet("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-(?:(\\d+)|(.+))$)?}")]
        public async Task<IActionResult> GetNode(int ds_id, string node_id = "0-85")
        {
            if (ds_id < 0 || ds_id >= _UAServers.Length) return NotFound($"There is no Data Set for id {ds_id}");
            
            var serverUrl = _UAServers[ds_id].Url;
            if (!(await _UAClient.IsServerAvailable(serverUrl)))
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");

            var decodedNodeId = WebUtility.UrlDecode(node_id);
            
            Node sourceNode;
            try 
            {
			    sourceNode = await _UAClient.ReadNodeAsync(serverUrl, decodedNodeId);
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
                    default:
                        return StatusCode(500, new
                        {
                            error = exc.Message
                        });
                }
            }
            catch (DataSetNotAvailableException exc)
            {
                return StatusCode(500, "Data Set " + ds_id + " NotAvailable");
            }

            JObject result = new JObject();
            result["node-id"] = decodedNodeId;
            result["name"] = sourceNode.DisplayName.Text;

            switch (sourceNode.NodeClass)
            {
                case NodeClass.Method:
                    result["type"] = "method";
                    break;
                case NodeClass.Variable:
                    result["type"] = "variable";
                    var varNode = (VariableNode) sourceNode;
                    var uaValue = await _UAClient.ReadUaValueAsync(serverUrl, varNode);
                    result["value"] = uaValue.Value;
                    result["value-schema"] = JObject.Parse(uaValue.Schema.ToString());
                    result["status"] = uaValue.StatusCode?.ToString() ?? "";
                    result["deadBand"] = await _UAClient.GetDeadBandAsync(serverUrl, varNode);
                    result["minimumSamplingInterval"] = varNode.MinimumSamplingInterval;
                    break;
                case NodeClass.Object:
                    result["type"] = await _UAClient.IsFolderTypeAsync(serverUrl, decodedNodeId) ? "folder" : "object";
                    break;
            }

            var linkedNodes = new JArray();
            var refDescriptions = await _UAClient.BrowseAsync(serverUrl, decodedNodeId);
            foreach (var rd in refDescriptions)
            {
                var refTypeNode = await _UAClient.ReadNodeAsync(serverUrl, rd.ReferenceTypeId);
                var targetNode = new JObject
                {
                    //["node-id"] = rd.NodeId.ToStringId(),
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
                        targetNode["Type"] = await _UAClient.IsFolderTypeAsync(serverUrl, rd.PlatformNodeId)
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
            
            return Ok(result);
        }

        [HttpPut("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-(?:(\\d+)|(.+))$)?}")]
        public async Task<IActionResult> PutNodeAsync(int ds_id, string node_id, [FromBody] VariableState state)
        {
            if (state == null || !state.isValid)
                return BadRequest(new
                {
                    error = "Insert a valid state for a Variable Node."
                });

            if (ds_id < 0 || ds_id >= _UAServers.Length) return NotFound($"There is no Data Set for id {ds_id}");

            var serverUrl = _UAServers[ds_id].Url;
            if (!(await _UAClient.IsServerAvailable(serverUrl)))
                return StatusCode(500, new
                {
                    error = "Data Set " + ds_id + " NotAvailable"
                });

            var decodedNodeId = WebUtility.UrlDecode(node_id);

            Node sourceNode;
            try
            {
                sourceNode = await _UAClient.ReadNodeAsync(serverUrl, decodedNodeId);
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
                    default:
                        return StatusCode(500, new
                        {
                            error = exc.Message
                        } );
                }
            }
            catch (DataSetNotAvailableException exc)
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
                await _UAClient.WriteNodeValueAsync(serverUrl, variableNode, state);
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
                return BadRequest(new
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
            if (ds_id < 0 || ds_id >= _UAServers.Length) return NotFound($"There is no Data Set for id {ds_id}");

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
            
            var serverUrl = _UAServers[ds_id].Url;
            bool[] results;
            try 
            {
                results = await _UAClient.CreateMonitoredItemsAsync(serverUrl, 
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
                    default:
                        return StatusCode(500, exc.Message);
                }
            }
            catch(DataSetNotAvailableException exc)
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
            if (ds_id < 0 || ds_id >= _UAServers.Length) return NotFound($"There is no Data Set for id {ds_id}");
            
            if (stopMonitorParams == null || !stopMonitorParams.IsValid())
            {
                return BadRequest(new
                {
                    error = "Bad parameters format."
                });
            }
            
            var serverUrl = _UAServers[ds_id].Url;
            var result = await _UAClient.DeleteMonitoringPublish(serverUrl, stopMonitorParams.BrokerUrl,
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
