using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebPlatform.Models.Auth;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        // GET: api/values
        [HttpPost("authenticate")]
        public IActionResult Get([FromForm]AuthCredentials cred)
        {
            if (!cred.isValid) { return BadRequest("Inserisci username e password"); }
            return Ok($"Autenticato {cred.Username} con password {cred.Password}");
        }


    }
}
