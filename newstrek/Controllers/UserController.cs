using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace newstrek.Controllers
{
    public class UserSignUpModel
    {
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public InterestedTopicDto InterestedTopicDto { get; set; }
    }

    public class InterestedTopicDto
    {
        public bool? world { get; set; } = false;
        public bool? business { get; set; } = false;
        public bool? politics { get; set; } = false;
        public bool? health { get; set; } = false;
        public bool? climate { get; set; } = false;
        public bool? tech { get; set; } = false;
        public bool? entertainment { get; set; } = false;
        public bool? science { get; set; } = false;
        public bool? history { get; set; } = false;
        public bool? sports { get; set; } = false;
    }

    public class UserSignInModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly NewsTrekDbContext _newsTrekDbContext;

        public UserController(IConfiguration configuration, NewsTrekDbContext newsTrekDbContext)
        {
            _configuration = configuration;
            _newsTrekDbContext = newsTrekDbContext;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] UserSignUpModel user)
        {
            if (user == null)
            {
                return BadRequest("Invalid client request");
            }

            if (!ModelState.IsValid)
            {
                // Model is not valid, return validation errors to the frontend
                return BadRequest(new { error = "Email is not valid" });
            }

            if (user.Password != user.ConfirmPassword)
            {
                return BadRequest(new { error = "Confirm your password" });
            }

            var findEmail = await _newsTrekDbContext.Users.FirstOrDefaultAsync(f => f.Email == user.Email);
            if (findEmail != null)
            {
                return BadRequest(new { error = "Email is already been signed up" });
            }

            var newUser = new User
            {
                Name = user.Name,
                Email = user.Email,
                Password = HashPassword(user.Password),
                Provider = "native",
                AccessExpired = Convert.ToInt32(_configuration["Jwt:ExpirationSeconds"]),
                // AccessToken = token,
                LoginAt = DateTime.Now,
                InterestedTopic = new InterestedTopic
                {
                    world = user.InterestedTopicDto.world,
                    business = user.InterestedTopicDto.business,
                    politics = user.InterestedTopicDto.politics,
                    health = user.InterestedTopicDto.health,
                    climate = user.InterestedTopicDto.climate,
                    tech = user.InterestedTopicDto.tech,
                    entertainment = user.InterestedTopicDto.entertainment,
                    science = user.InterestedTopicDto.science,
                    history = user.InterestedTopicDto.history,
                    sports = user.InterestedTopicDto.sports
                }
            };

            var token = GenerateJwtToken(newUser);
            newUser.AccessToken = token;

            try
            {
                Console.WriteLine(newUser);
                await _newsTrekDbContext.Users.AddAsync(newUser);
                await _newsTrekDbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(new
                {
                    error = e.Message
                });
            }

            return Ok(new
            {
                accessToken = token,
                accessExpired = newUser.AccessExpired,
                loginAt = newUser.LoginAt,
                user = new
                {
                    id = newUser.Id,
                    name = newUser.Name,
                    email = newUser.Email,
                    provider = newUser.Provider,
                    interestedTopic = newUser.InterestedTopic
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] UserSignInModel user)
        {
            if (user == null)
            {
                return BadRequest("Invalid client request");
            }

            var expectedUser = await _newsTrekDbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);

            if (expectedUser == null)
            {
                return NotFound(new
                {
                    error = "User not found. Check your Email.",
                    message = "Please sign up first."
                });
            }

            if (!VerifyPassword(user.Password, expectedUser.Password))
            {
                return BadRequest(new
                {
                    error = "Invalid password.",
                    message = "Please try again."
                });
            }

            var token = GenerateJwtToken(expectedUser);

            return Ok(new
            {
                accessToken = token,
                accessExpired = expectedUser.AccessExpired,
                loginAt = expectedUser.LoginAt,
                user = new
                {
                    id = expectedUser.Id,
                    name = expectedUser.Name,
                    email = expectedUser.Email,
                    provider = expectedUser.Provider
                }
            });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserSignUpModel>> GetProfile()
        {
            try
            {
                var userIdentity = HttpContext.User.Identity as ClaimsIdentity;
                var email = userIdentity.FindFirst(ClaimTypes.Email)?.Value;
                var name = userIdentity.FindFirst("name")?.Value;
                //var picture = userIdentity.FindFirst("picture")?.Value;
                //var provider = userIdentity.FindFirst("provider")?.Value;

                var userProfile = new UserSignUpModel
                {
                    Name = name,
                    Email = email,
                };

                return Ok(userProfile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        private string GenerateJwtToken(User user)
        {
            DotNetEnv.Env.Load();
            var JWT_Issuer_SigningKey = System.Environment.GetEnvironmentVariable("JWT_Issuer_SigningKey");
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_Issuer_SigningKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("provider", user.Provider),
                new Claim("name", user.Name)
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddSeconds(Convert.ToInt32(_configuration["Jwt:ExpirationSeconds"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string HashPassword(string password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);

            var hashedPassword = new Rfc2898DeriveBytes(password, salt, 10000).GetBytes(20);

            var hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hashedPassword, 0, hashBytes, 16, 20);

            var hashedPasswordString = Convert.ToBase64String(hashBytes);

            return hashedPasswordString;
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);

            var salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);

            var hashedPasswordToCheck = new Rfc2898DeriveBytes(password, salt, 10000).GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hashedPasswordToCheck[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

