﻿using newstrek.Data;
using Microsoft.AspNetCore.Mvc;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using newstrek.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        // Key for Collegiate® Dictionary with Audio
        string? DictionaryApiKey = Environment.GetEnvironmentVariable("DictionaryApiKey");

        [HttpGet("look-up-words")]
        public async Task<IActionResult> LookUpWords(string word)
        {
            string? DictionaryRequestUrl = $"https://www.dictionaryapi.com/api/v3/references/collegiate/json/{word}?key={DictionaryApiKey}";

            try
            {
                // Create an HttpClient instance using the factory
                var httpClient = _httpClientFactory.CreateClient();
                // Send the request to Webster Collegiate® Dictionary with Audio API
                var response = await httpClient.GetAsync(DictionaryRequestUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    dynamic result = JsonSerializer.Deserialize<dynamic>(responseBody);

                    return Ok(result);
                }
                else
                {
                    // Handle non-success status codes
                    return StatusCode((int)response.StatusCode);
                }
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
            var userId = _jwtParseService.ParseUserId();

            if (userId == 0)
            {
                return BadRequest("userId claim is missing or invalid");
            }

            bool vocabularyIsExist = await _newsTrekDbContext.Vocabularies.AnyAsync(v => v.Word == word && v.UserId == userId);

            if (!vocabularyIsExist)
            {
                await _newsTrekDbContext.Vocabularies.AddAsync(new Vocabulary { Word = word, UserId = userId });
                await _newsTrekDbContext.SaveChangesAsync();

                return Ok(new { response = $"Vocabulary \"{word}\" saved" });
            }

            return Ok(new { response = $"Vocabulary \"{word}\" is already saved in database" });
        }

        [HttpDelete("delete-saved-vocabulary")]
        public async Task<IActionResult> DeleteSavedVocabulary([FromQuery] string? word)
        {
            try
            {
                var email = _jwtParseService.ParseEmail();

                var vocabularyToDelete = await _newsTrekDbContext.Users
                    .Where(u => u.Email == email)
                    .SelectMany(u => u.Vocabularies)
                    .Where(v => v.Word == word)
                    .FirstOrDefaultAsync();

                // "Remove" method does not return a task that can be awaited. It's a synchronous operation, and you don't need to await it
                _newsTrekDbContext.Vocabularies.Remove(vocabularyToDelete);
                await _newsTrekDbContext.SaveChangesAsync();

                return Ok(new { Result = "Deletion complete" });
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return BadRequest(ex.Message);
            }
        }
    }
}
