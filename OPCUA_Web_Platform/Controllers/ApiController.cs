using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebPlatform.Models.DataSet;
using WebPlatform.Models.OptionsModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private OPCUAServers[] _UAServers;

        public ApiController(IOptions<OPCUAServersOptions> servers)
        {
            this._UAServers = servers.Value.Servers;
            for (int i = 0; i < _UAServers.Length; i++) _UAServers[i].Id = i;
            
        }

        [HttpGet("data-sets")]
        public IActionResult GetDataSets()
        {
            return Ok( _UAServers );
        }

        [HttpGet("data-sets/{ds_id:int}/nodes/{node_id:regex(^\\d+-\\S+$)?}")]
        public IActionResult GetNode(int ds_id, string node_id = "0-85")
        {
            return Ok($"Ti ritorno il nodo {node_id} del data set {ds_id}");
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
