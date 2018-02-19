using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebPlatform.Auth;
using WebPlatform.Models.Auth;
using WebPlatform.Models.OptionsModels;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebPlatform.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private IAuth _auth;
        private ITokenManager _jwtManager;

        public AuthController(ITokenManager jwtManager, IAuth auth) 
        {
            _jwtManager = jwtManager;
            _auth = auth;
        }

        // GET: api/values
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Get([FromForm]AuthCredentials authCreds)
        {
            if (!authCreds.isValid) { return BadRequest("Inserisci username e password"); }

            if(_auth.AuthenticateWithCredentials(authCreds.Username, authCreds.Password))
            {
                
                return Ok(new { token = _jwtManager.GenerateTokenForUser(authCreds.Username) });
            }

            return Unauthorized();
        }


    }
}
