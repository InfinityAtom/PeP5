using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PeP.Models;
using PeP.Services;

namespace PeP.Controllers
{
    [ApiController]
    [Route("api/exam-app")]
    public class ExamAppController : ControllerBase
    {
        private readonly IExamAppService _examAppService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamAppController(IExamAppService examAppService, UserManager<ApplicationUser> userManager)
        {
            _examAppService = examAppService;
            _userManager = userManager;
        }

        [HttpGet("code/{code}")]
        [Authorize(Roles = UserRoles.Student)]
        public async Task<ActionResult<ExamCodeInfoResponse>> GetExamInfo([FromRoute] string code)
        {
            var info = await _examAppService.GetExamInfoForCodeAsync(code);
            if (info == null)
            {
                return NotFound(new ExamCodeInfoResponse(false, "Invalid or expired exam code.", null));
            }

            return Ok(new ExamCodeInfoResponse(true, null, info));
        }

        [HttpPost("authorize")]
        [Authorize(Roles = UserRoles.Student)]
        public async Task<ActionResult<AuthorizeResponse>> Authorize([FromBody] AuthorizeRequest request)
        {
            var studentId = _userManager.GetUserId(User) ?? string.Empty;
            var result = await _examAppService.AuthorizeAsync(studentId, request.Code ?? string.Empty, request.TeacherPassword ?? string.Empty);

            if (!result.Success)
            {
                return BadRequest(new AuthorizeResponse(false, result.Error ?? "Authorization failed.", null, null, null));
            }

            return Ok(new AuthorizeResponse(true, null, result.AuthorizationToken, result.ExpiresAtUtc, result.Exam));
        }

        [HttpPost("start")]
        [Authorize(Roles = UserRoles.Student)]
        public async Task<ActionResult<StartResponse>> Start([FromBody] StartRequest request)
        {
            var studentId = _userManager.GetUserId(User) ?? string.Empty;
            var result = await _examAppService.StartAsync(studentId, request.AuthorizationToken ?? string.Empty);

            if (!result.Success || result.AttemptId == null || result.LaunchToken == null || result.ExpiresAtUtc == null)
            {
                return BadRequest(new StartResponse(false, result.Error ?? "Start failed.", null, null, null));
            }

            return Ok(new StartResponse(true, null, result.AttemptId, result.LaunchToken, result.ExpiresAtUtc));
        }

        public record ExamCodeInfoResponse(bool Success, string? Error, ExamAppExamInfo? Exam);

        public record AuthorizeRequest(string? Code, string? TeacherPassword);

        public record AuthorizeResponse(bool Success, string? Error, string? AuthorizationToken, DateTime? ExpiresAtUtc, ExamAppExamInfo? Exam);

        public record StartRequest(string? AuthorizationToken);

        public record StartResponse(bool Success, string? Error, int? AttemptId, string? LaunchToken, DateTime? ExpiresAtUtc);
    }
}

