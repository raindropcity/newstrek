using newstrek.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using newstrek.Services;

namespace crawler_test.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly RedisCacheManager _redisCacheManager;
        private readonly VocabularyService _vocabularyService;
        private readonly JwtParseService _jwtParseService;

        public DictionaryController (NewsTrekDbContext newsTrekDbContext, IHttpClientFactory httpClientFactory, RedisCacheManager redisCacheManager, VocabularyService vocabularyService, JwtParseService jwtParseService)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _httpClientFactory = httpClientFactory;
            _redisCacheManager = redisCacheManager;
            _vocabularyService = vocabularyService;
            _jwtParseService = jwtParseService;
        }

        // 爬蟲英文辭典網頁HTML結構
        [HttpGet("look-up-words-crawler-Merriam-Webster")]
        public async Task<IActionResult> LookUpWordsCrawlerMerriamWebster(string word)
        {
            try
            {
                var cacheKey = $"MerriamWebster_{word}";
                var address = $"https://www.merriam-webster.com/dictionary/{word}";
                var className = ".entry-word-section-container";

                var htmlStructure = await _vocabularyService.VocabularyCrawlerAsync(cacheKey, address, className);

                return Ok(htmlStructure);
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

        [HttpGet("look-up-words-crawler-Longman")]
        public async Task<IActionResult> LookUpWordsCrawlerLongman(string word)
        {
            try
            {
                var cacheKey = $"Longman_{word}";
                var address = $"https://www.ldoceonline.com/dictionary/{word}";
                var className = ".ldoceEntry";

                var htmlStructure = await _vocabularyService.VocabularyCrawlerAsync(cacheKey, address, className);

                return Ok(htmlStructure);
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

        /* 串接英文辭典API */
        [HttpGet("look-up-words")]
        public async Task<IActionResult> LookUpWords(string word)
        {
            try
            {
                dynamic result = await _vocabularyService.LookUPWebsterDictionaryAsync(word);

                return Ok(result);
            }
            catch (SecurityTokenExpiredException)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "The authentication token has expired.");
            }
            catch (SecurityTokenValidationException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "The authentication token is invalid.");
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("save-vocabulary")]
        public async Task<IActionResult> SaveVocabulary([FromQuery] string? word)
        {
            try
            {
                bool saveVocabulary = await _vocabularyService.SaveVocabularyIfNotExistsAsync(word);

                if (saveVocabulary)
                {
                    return Ok(new { response = $"Vocabulary \"{word}\" saved" });
                }

                return Ok(new { response = $"Vocabulary \"{word}\" is already saved in database" });
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
                return BadRequest(e.Message);
            }
        }

        [HttpDelete("delete-saved-vocabulary")]
        public async Task<IActionResult> DeleteSavedVocabulary([FromQuery] string? word)
        {
            try
            {
                bool vocabularyDeleted = await _vocabularyService.DeleteVocabularyAsync(word);

                if (vocabularyDeleted)
                {
                    return Ok(new { Result = $"Vocabulary \"{word}\" deleted" });
                }
                else
                {
                    return BadRequest(new { Result = $"Vocabulary \"{word}\" not found" });
                }
            }
            catch (SecurityTokenExpiredException)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, "The authentication token has expired.");
            }
            catch (SecurityTokenValidationException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, "The authentication token is invalid.");
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return BadRequest(ex.Message);
            }
        }
    }
}
