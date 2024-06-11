using FSMExtension.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FSMExtension.Controllers
{
    /// <summary>
    /// The extension's authentication handler, responsible for authenticating
    /// access to the extension's APIs.
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        /// <summary>
        /// Gets or creates a new application auth token needed to make calls to this extension's API.
        /// 
        /// Required headers:
        ///     "X-Cloud-Host": e.g., https://eu.coresuite.com
        ///     "X-Account-Name": the SAP FSM account name
        ///     "X-User-ID": the SAP FSM user ID
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Produces("application/json")]
        public async Task<IActionResult> GetAppToken()
        {
            // Read in required header values
            if (!Request.Headers.TryGetValue("X-Cloud-Host", out var cloudHost))
                return Forbid();
            if (!Request.Headers.TryGetValue("X-Account-Name", out var accountName))
                return Forbid();
            if (!Request.Headers.TryGetValue("X-User-ID", out var userId))
                return Forbid();

            var incomingAuth = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(incomingAuth))
                return Forbid();
            if (!incomingAuth.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var incomingToken = incomingAuth[7..];
            var (user, appToken) = await authService.GetAppTokenAsync(cloudHost!, accountName!, userId!, incomingToken);
            if (user == null || appToken == null)
                return Forbid();

            return Ok(new { token = appToken, email = user.Email });
        }
    }
}
