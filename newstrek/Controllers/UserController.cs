using newstrek.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using newstrek.Services;
using newstrek.Dto;

namespace newstrek.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly UserService _userService;

        public UserController(NewsTrekDbContext newsTrekDbContext, UserService userService)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _userService = userService;
        }

        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<IActionResult> SignUpAsync([FromBody] UserSignUpModel user)
        {
            try
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

                var newUser = await _userService.UserSignUpAsync(user);

                return Ok(new
                {
                    accessToken = newUser.AccessToken,
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
            catch (Exception e)
            {
                return BadRequest(new
                {
                    error = e.Message
                });
            }
        }

        [AllowAnonymous]
        [HttpPost("signin")]
        public async Task<IActionResult> SignInAsync([FromBody] UserSignInModel user)
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

            if (!_userService.VerifyPassword(user.Password, expectedUser.Password))
            {
                return BadRequest(new
                {
                    error = "Invalid password.",
                    message = "Please try again."
                });
            }

            var token = _userService.GenerateJwtToken(expectedUser);

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

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult> GetProfileAsync()
        {
            try
            {
                var userProfile = await _userService.GetUserProfileAsync();

                return Ok(userProfile);
            }
            catch (SecurityTokenExpiredException)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "The authentication token has expired.");
            }
            catch (SecurityTokenValidationException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "The authentication token is invalid.");
            }
            catch (Exception e)
            {
                return StatusCode(500, "Internal Server Error: " + e.Message);
            }
        }

        [HttpPut("modify-interested-topic")]
        public async Task<IActionResult> ModifyInterestedTopicAsync([FromBody] InterestedTopicDto selectedTopic)
        {
            try
            {
                var modifyResult = await _userService.UserModifyInterestedTopicAsync(selectedTopic);

                return Ok(modifyResult);
            }
            catch (SecurityTokenExpiredException)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "The authentication token has expired.");
            }
            catch (SecurityTokenValidationException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "The authentication token is invalid.");
            }
            catch (Exception e)
            {
                return StatusCode(500, "Internal Server Error: " + e.Message);
            }
        }
    }
}
