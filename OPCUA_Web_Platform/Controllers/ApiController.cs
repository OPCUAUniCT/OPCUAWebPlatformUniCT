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
using WebPlatform.OPCUALayer;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private OPCUAServers[] _UAServers;
        private IUAClientSingleton _UAClient;

        public ApiController(IOptions<OPCUAServersOptions> servers, IUAClientSingleton UAClient)
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

        [HttpGet("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-(?:(\\d+)|(\\S+))$)?}")]
        public async Task<IActionResult> GetNode(int ds_id, string node_id = "0-85")
        {
            if (ds_id < 0 || ds_id >= _UAServers.Length) return NotFound($"There is no Data Set for id {ds_id}");
            var serverUrl = _UAServers[ds_id].Url;
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
                        return NotFound("There is no node with the specified Node Id");
                    case StatusCodes.BadNodeIdInvalid:
                        return BadRequest("Provided Node Id is invalid");
                    default:
                        throw exc;
                }
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
                    //TODO: gestire tutta la decodifica delle variabili. Creare un nuovo pbi;
                    var varNode = (VariableNode) sourceNode;
                    var uaValue = await _UAClient.ReadUaValueAsync(serverUrl, varNode);
                    result["value"] = uaValue.Value;
                    //result["value-schema"] = JObject.Parse(uaValue.Schema.ToString());
                    //TODO: le due righe successive sono una prova da eliminare
                    var schema4 = JsonSchema4.FromSampleJson(uaValue.Value.ToString());
                    result["value-schema"] = JObject.Parse(schema4.ToJson());
                    break;
                case NodeClass.Object:
                    result["type"] = await _UAClient.IsFolderTypeAsync(serverUrl, decodedNodeId) ? "folder" : "object";
                    break;
            }

            var linkedNodes = new JArray();
            var refDescriptions = await _UAClient.BrowseAsync(serverUrl, decodedNodeId);
            foreach (ReferenceDescription rd in refDescriptions)
            {
                Node refTypeNode = await _UAClient.ReadNodeAsync(serverUrl, rd.ReferenceTypeId);
                var targetNode = new JObject();

                targetNode["node-id"] = rd.NodeId.ToStringId();
                targetNode["name"] = rd.DisplayName.Text;

                switch (rd.NodeClass)
                {
                    case NodeClass.Variable:
                        targetNode["Type"] = "variable";
                        break;
                    case NodeClass.Method:
                        targetNode["Type"] = "method";
                        break;
                    case NodeClass.Object:
                        targetNode["Type"] = await _UAClient.IsFolderTypeAsync(serverUrl, rd.NodeId.ToStringId())
                            ? "folder"
                            : "object";
                        break;
                }
                
                targetNode["relationship"] = refTypeNode.DisplayName.Text;
                
                linkedNodes.Add(targetNode);
            }

            result["edges"] = linkedNodes;
            
            return Ok(result);
        }

        [HttpPut("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-\\S+$)}")]
        public IActionResult PutNode(int ds_id, string node_id, [FromForm] VariableState state)
        {
            if (!state.isValid) { return BadRequest("Insert a valid state for a Variable Node."); }
            return Ok($"Scrivo {state.Value} sul nodo {node_id} del data set {ds_id}");
        }

        [HttpPost("data-sets/{ds_id:int}/monitor")]
        public IActionResult Monitor(int ds_id)
        {
            return Ok("Monitoro");
        }

        [HttpPost("data-sets/{ds_id:int}/stop-monitor")]
        public IActionResult StopMonitor(int ds_id)
        {
            return Ok($"Smonitoro");
        }

    }
}
