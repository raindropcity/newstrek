﻿using newstrek.Data;
using Microsoft.AspNetCore.Mvc;
using AngleSharp;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace crawler_test.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public DictionaryController (NewsTrekDbContext newsTrekDbContext, IHttpClientFactory httpClientFactory)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _httpClientFactory = httpClientFactory;
        }

        // 爬蟲英文辭典網頁HTML結構
        [HttpGet("look-up-words-crawler-Merriam-Webster")]
        public async Task<IActionResult> LookUpWordsCrawlerMerriamWebster(string word)
        {
            try
            {
                var config = Configuration.Default.WithDefaultLoader();
                var address = $"https://www.merriam-webster.com/dictionary/{word}";
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);

                var def = document.QuerySelectorAll(".entry-word-section-container");
                string? htmlStructure = "";
                foreach (var item in def)
                {
                    htmlStructure += item.OuterHtml;
                }

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
                var config = Configuration.Default.WithDefaultLoader();
                var address = $"https://www.ldoceonline.com/dictionary/{word}";
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);

                var def = document.QuerySelectorAll(".ldoceEntry");
                string? htmlStructure = "";
                foreach (var item in def)
                {
                    htmlStructure += item.OuterHtml;
                }

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

        /* 串接英文辭典API：棄用 */
        // Key for Collegiate® Dictionary with Audio / Collegiate® Thesaurus API
        string? DictionaryApiKey = Environment.GetEnvironmentVariable("DictionaryApiKey");
        string? ThesaurusApiKey = Environment.GetEnvironmentVariable("ThesaurusApiKey");

        string? wordnikApiKey = Environment.GetEnvironmentVariable("wordnikApiKey");

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
    }
}
