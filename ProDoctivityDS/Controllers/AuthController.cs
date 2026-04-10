using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity.Login;
using ProDoctivityDS.Application.Interfaces;
using ProDoctivityDS.Domain.Interfaces;

namespace ProDoctivityDS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ICurrentUserService currentUserService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        private string GetOrCreateSessionId()
        {
            if (Request.Headers.TryGetValue("X-Session-Id", out var sessionId))
                return sessionId.ToString();

            var newSessionId = Guid.NewGuid().ToString();
            Response.Headers.Append("X-Session-Id", newSessionId);
            return newSessionId;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Usuario y contraseña son requeridos" });

            var sessionId = GetOrCreateSessionId();
            var result = await _authService.LoginAsync(request.Username, request.Password, sessionId, cancellationToken);
            if (result.Success)
            {
                return Ok(new { message = result.Message, token = result.Token });
            }
            else
            {
                return Unauthorized(new { message = result.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var sessionId = GetOrCreateSessionId();
            await _authService.LogoutAsync(cancellationToken);
            _currentUserService.RemoveSession(sessionId);
            return Ok(new { message = "Sesión cerrada" });
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status(CancellationToken cancellationToken)
        {
            var status = await _authService.GetAuthStatusAsync(cancellationToken);
            return Ok(status);
        }
    }
}