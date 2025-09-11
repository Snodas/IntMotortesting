using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JwtTestController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public JwtTestController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("public")]
        public IActionResult Public()
        {
            return Ok(new
            {
                message = "This endpoint is public - no authentication required",
                timestamp = DateTime.Now,
                status = "success"
            });
        }

        [HttpGet("protected")]
        [Authorize]
        public IActionResult Protected()
        {
            var claims = User.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value
            }).ToList();

            return Ok(new
            {
                message = "This endpoint requires authentication",
                user = User.Identity?.Name ?? "Unknown",
                isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                claims = claims,
                timestamp = DateTime.Now,
                status = "authenticated"
            });
        }

        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult Admin()
        {
            return Ok(new
            {
                message = "This endpoint requires Admin role",
                user = User.Identity?.Name ?? "Unknown",
                timestamp = DateTime.Now,
                status = "admin_access"
            });
        }

        [HttpGet("genereate-test-token")]
        public IActionResult GenerateTestToken([FromQuery] string role = "user")
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound("This endpoint is only available in development");
            }

            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this-is-a-very-long-secret-key-for-testing-jwt-tokens-in-development-only"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "testuser@company.com"),
                    new Claim(ClaimTypes.NameIdentifier, "12345"),
                    new Claim(ClaimTypes.Email, "testuser@company.com"),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("sub", "12345"),
                    new Claim("preferred_username", "testuser"),
                };

                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    claims.Add(new Claim("permissions", "jobs-manage"));
                    claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                }

                var token = new JwtSecurityToken(
                    issuer: "test-issuer",
                    audience: "test-audience",
                    claims: claims,
                    expires: DateTime.Now.AddHours(2),
                    signingCredentials: creds
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    token = tokenString,
                    expires = DateTime.Now.AddHours(2),
                    user = "testuser@company.com",
                    role = role,
                    intructions = "Copy this token and use it in the 'Authorize' button!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("token-info")]
        public IActionResult TokenInfo()
        {
            //Debug
            var authHeader = Request.Headers["Authoriztion"].FirstOrDefault();
            var authHeaderLower = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) && string.IsNullOrEmpty(authHeaderLower))
            {
                return BadRequest(new
                {
                    //error = "No Authorization header found",
                    //allHeaders = Request.Headers.Select(h => new { Key = h.Key, Value = h.})
                }); 
            }

            var token = Request.Headers["Authorizaion"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("no token provided");
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);

                return Ok(new
                {
                    header = jsonToken.Header,
                    payload = jsonToken.Payload,
                    validFrom = jsonToken.ValidFrom,
                    validTo = jsonToken.ValidTo,
                    issuer = jsonToken.Issuer,
                    audiences = jsonToken.Audiences,
                    claims = jsonToken.Claims
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Invalid token", details = ex.Message });
            }
        }
    }
}
