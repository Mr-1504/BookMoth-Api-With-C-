using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.RequestModels;
using BookMoth_Api_With_C_.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookMoth_Api_With_C_.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly FcmService _fcmService;
        private readonly BookMothContext _context;

        public NotificationController(FcmService fcmService, BookMothContext context)
        {
            _fcmService = fcmService;
            _context = context;
        }

        //[HttpPost("send")]
        //public async Task<IActionResult> SendNotification([FromBody] NotificationRequest request)
        //{
        //    await _fcmService.SendNotificationAsync(request.Token, request.Title, request.Body);
        //    return Ok();
        //}

        [HttpPost("register")]
        public async Task<IActionResult> RegisterToken([FromBody] FcmTokenRequest model)
        {
            if (string.IsNullOrEmpty(model.Token) || string.IsNullOrEmpty(model.DeviceId))
                return BadRequest();

            var accId = User.FindFirst("accountId")?.Value;
            if (accId == null)
            {
                return Unauthorized(new {error_code = "INVALID_TOKEN" });
            }

            var accountId = int.Parse(accId);

            var existingToken = await _context.FcmTokens.FirstOrDefaultAsync(t =>
               ( t.DeviceId == model.DeviceId && t.AccountId == accountId)
            );
            if (existingToken != null)
            {
                existingToken.Token = model.Token;
            }
            else
            {
                _context.FcmTokens.Add(new FcmToken
                {
                    AccountId = accountId,
                    DeviceId = model.DeviceId,
                    Token = model.Token
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

    }
}
