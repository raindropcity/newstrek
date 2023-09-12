using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;
using newstrek.Services;

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
        private readonly JwtParseService _jwtParseService;

        public UserController(IConfiguration configuration, NewsTrekDbContext newsTrekDbContext, JwtParseService jwtParseService)
        {
            _configuration = configuration;
            _newsTrekDbContext = newsTrekDbContext;
            _jwtParseService = jwtParseService;
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
        public async Task<ActionResult> GetProfile()
        {
            try
            {
                var email = _jwtParseService.ParseEmail();
                var name = _jwtParseService.ParseUsername();

                var savedVocabulary = await _newsTrekDbContext.Users
                                        .Where(u => u.Email == email)
                                        .SelectMany(u => u.Vocabularies) // Flatten the nested collection
                                        .Select(v => v.Word) // Select the "Word" field
                                        .ToListAsync();

                var userInterestedTopic = await _newsTrekDbContext.Users.Where(u => u.Email == email).Select(s => s.InterestedTopic).ToListAsync();

                // Try to iterate the object(must use System.Reflection)
                Type objectType = typeof(InterestedTopic);
                PropertyInfo[] properties = objectType.GetProperties();
                List<string> selectedInterestedTopic = new List<string>();

                foreach (PropertyInfo property in properties)
                {
                    string propertyName = property.Name;
                    object propertyValue = property.GetValue(userInterestedTopic[0]);

                    // Check if propertyValue is a boolean and true
                    if (propertyValue is bool && (bool)propertyValue)
                    {
                        // If the value is true, add its key into the List selectedInterestedTopic
                        selectedInterestedTopic.Add(propertyName);
                    }
                }

                return Ok(new { 
                    Name = name, 
                    UserInterestedTopic = selectedInterestedTopic,
                    UserSavedVocabulary = savedVocabulary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("modify-interested-topic")]
        public async Task<IActionResult> ModifyInterestedTopic([FromBody] InterestedTopicDto selectedTopic)
        {
            var email = _jwtParseService.ParseEmail();

            var specificUserInterestedTopic = await _newsTrekDbContext.Users
                .Where(u => u.Email == email)
                .Select(s => s.InterestedTopic)
                .SingleOrDefaultAsync();

            if (specificUserInterestedTopic != null)
            {
                specificUserInterestedTopic.world = selectedTopic.world;
                specificUserInterestedTopic.business = selectedTopic.business;
                specificUserInterestedTopic.politics = selectedTopic.politics;
                specificUserInterestedTopic.health = selectedTopic.health;
                specificUserInterestedTopic.climate = selectedTopic.climate;
                specificUserInterestedTopic.tech = selectedTopic.tech;
                specificUserInterestedTopic.entertainment = selectedTopic.entertainment;
                specificUserInterestedTopic.science = selectedTopic.science;
                specificUserInterestedTopic.history = selectedTopic.history;
                specificUserInterestedTopic.sports = selectedTopic.sports;

                await _newsTrekDbContext.SaveChangesAsync();
            }

            return Ok(new { 
                response = "Modification saved",
                userInterestedTopic = specificUserInterestedTopic
            });
        }

        private string GenerateJwtToken(User user)
        {
            var JWT_Issuer_SigningKey = _configuration["Jwt:SecretKey"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_Issuer_SigningKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Calculate the Unix Epoch time (seconds since January 1, 1970, UTC)
            var issuedAtUtc = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "TokenForTheApiWithAuth"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, issuedAtUtc.ToString(), ClaimValueTypes.Integer64),
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
