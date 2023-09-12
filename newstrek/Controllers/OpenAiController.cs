using newstrek.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace crawler_test.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAiController : ControllerBase
    {
        private readonly ILogger<OpenAiController> _logger;
        private readonly IOpenAiService _openAiService;

        public OpenAiController (ILogger<OpenAiController> logger, IOpenAiService openAiService)
        {
            _logger = logger;
            _openAiService = openAiService;
        }

        [HttpPost("complete-sentence")]
        public async Task<IActionResult> CompleteSentence ([FromBody] HashSet<string> vocabularyList)
        {
            try
            {
                string text = string.Join(", ", vocabularyList);
                string question = $"Make a compelete sentence by: {text}.";
                Console.WriteLine (question);

                // make a request to OpenAi and get the response
                var result = await _openAiService.CompleteSentence(question);
                Console.WriteLine (result);
                return Ok(new { Result = result });
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
