using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Route("[controller]")]
    public class ApiController : Controller
    {
        [HttpGet("data-sources")]
        public IActionResult GetDataSources()
        {
            return Ok("Ti ritorno gli url dei Data Set");
        }

        [HttpGet("data-sources/{ds_id:int}/nodes")]
        public IActionResult GetDataSources(int ds_id)
        {
            //return Ok($"Ti ritorno l'entry point al Data Set {ds_id}");
            return GetDataSources(ds_id, "0-85");
        }

        [HttpGet("data-sources/{ds_id:int}/nodes/{node_id:regex(^\\d+-\\S+$)}")]
        public IActionResult GetDataSources(int ds_id, string node_id)
        {
            return Ok($"Ti ritorno il nodo {node_id} del datat set {ds_id}");
        }
    }
}
