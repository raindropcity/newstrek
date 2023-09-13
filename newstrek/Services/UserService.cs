using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using newstrek.Data;
using newstrek.Dto;
using newstrek.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace newstrek.Services
{
    public class UserService
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IConfiguration _configuration;
        private readonly JwtParseService _jwtParseService;
        private readonly MapObjectToListService _mapObjectToListService;

        public UserService (NewsTrekDbContext newsTrekDbContext, IConfiguration configuration, JwtParseService jwtParseService, MapObjectToListService mapObjectToListService)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _configuration = configuration;
            _jwtParseService = jwtParseService;
            _mapObjectToListService = mapObjectToListService;
        }

        public async Task<User> UserSignUpAsync (UserSignUpModel user) 
        {
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

            _newsTrekDbContext.Users.AddAsync(newUser);
            await _newsTrekDbContext.SaveChangesAsync();

            return newUser;
        }

        public async Task<object> GetUserProfileAsync ()
        {
            var email = _jwtParseService.ParseEmail();
            var name = _jwtParseService.ParseUsername();

            var savedVocabulary = await _newsTrekDbContext.Users
                                    .Where(u => u.Email == email)
                                    .SelectMany(u => u.Vocabularies) // Flatten the nested collection
                                    .Select(v => v.Word) // Select the "Word" field
                                    .ToListAsync();

            var userInterestedTopic = await _newsTrekDbContext.Users.Where(u => u.Email == email).Select(s => s.InterestedTopic).ToListAsync();

            List<string> selectedInterestedTopic = _mapObjectToListService.MapInterestedTopicToListAsync(userInterestedTopic);

            return new
            {
                Name = name,
                UserInterestedTopic = selectedInterestedTopic,
                UserSavedVocabulary = savedVocabulary
            };
        }

        public async Task<object> UserModifyInterestedTopicAsync (InterestedTopicDto selectedTopic) 
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

            return new
            {
                response = "Modification saved",
                userInterestedTopic = specificUserInterestedTopic
            };
        }

        public string GenerateJwtToken(User user)
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

        public string HashPassword(string password)
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

        public bool VerifyPassword(string password, string hashedPassword)
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
