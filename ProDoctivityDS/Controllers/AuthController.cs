using Microsoft.AspNetCore.Mvc;
using ProDoctivityDS.Application.Dtos.ProDoctivity.Login;
using ProDoctivityDS.Application.Interfaces;

namespace ProDoctivityDS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Usuario y contraseña son requeridos" });

            var result = await _authService.LoginAsync(request.Username, request.Password, cancellationToken);
            if (result.Success)
            {
                return Ok(new { message = result.Message, token = result.Token }); // token opcional
            }
            else
            {
                return Unauthorized(new { message = result.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(cancellationToken);
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