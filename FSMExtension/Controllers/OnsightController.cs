using FSMExtension.Models;
using FSMExtension.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FSMExtension.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class OnsightController(IAuthService authService, IContactService contactService) : ControllerBase
    {
        /// <summary>
        /// Given a SAP FSM DTO, return the contact information of persons associated with that DTO.
        /// 
        /// For example, if given an Activity, return the field worker and remote expert(s) assigned to that Activity.
        /// 
        /// Required: "Authorization" header - the bearer token returned by AuthController.GetAppToken().
        /// 
        /// </summary>
        /// <param name="dtoType">The type of FSM DTO currently selected and for which contact information is requested.</param>
        /// <param name="dtoId">The ID of the FSM DTO currently selected.</param>
        /// <returns></returns>
        [HttpGet("contacts")]
        public async Task<IActionResult> GetContacts([FromQuery(Name = "t")] string dtoType, [FromQuery(Name = "id")] string dtoId)
        {
            var user = await authService.GetUserAsync(Request.Headers.Authorization);
            if (user == null)
            {
                return Unauthorized();
            }

            // Lookup all relevant contacts for the given entity
            var clientCreds = ClientCredentials.GetFsm(user.CloudHost, user.AccountName);
            var contacts = await contactService.GetContactsAsync(user, clientCreds, dtoType, dtoId);
            return Ok(contacts);
        }

        /// <summary>
        /// Creates an Onsight NOW meeting, inviting the given participants.
        /// </summary>
        /// <param name="meetingRequest"></param>
        /// <returns></returns>
        [HttpPost("meetings")]
        [Consumes("application/json")]
        [Produces("text/plain")]
        public async Task<IActionResult> CreateMeeting([FromBody] MeetingRequest meetingRequest)
        {
            var user = await authService.GetUserAsync(Request.Headers.Authorization);
            if (user == null)
            {
                return Unauthorized();
            }

            var clientCreds = ClientCredentials.GetOnsightNow(user.CloudHost, user.AccountName);
            var nowClient = new OnsightNowClient(clientCreds);
            var meetingUrl = await nowClient.ScheduleMeetingAsync(meetingRequest);
            return Ok(meetingUrl);
        }
    }
}
