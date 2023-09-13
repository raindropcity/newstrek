using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;
using newstrek.Services;
using Microsoft.IdentityModel.Tokens;

namespace newstrek.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElasticSearchController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IElasticClient _elasticClient;
        private readonly JwtParseService _jwtParseService;
        private readonly ElasticSearchService _elasticSearchService;
        public ElasticSearchController(IElasticClient elasticClient, NewsTrekDbContext newsTrekDbContext, JwtParseService jwtParseService, ElasticSearchService elasticSearchService)
        {
            _elasticClient = elasticClient;
            _newsTrekDbContext = newsTrekDbContext;
            _jwtParseService = jwtParseService;
            _elasticSearchService = elasticSearchService;
        }

        [Authorize]
        [HttpGet("search-news")] // 要分查詢權重
        public async Task<IActionResult> SearchNewsAsync(string keyword)
        {
            var results = await _elasticSearchService.ElasticSearchNewsAsync(keyword);

            try
            {
                if (results.Documents.ToList().Count > 0)
                {
                    return Ok(new { response = "Query successfully", result = results.Documents.ToList() });
                }

                return Ok(new { response = "No query results, please check your keyword" });
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

        [Authorize]
        [HttpGet("search-news-by-category")]
        public async Task<IActionResult> SearchNewsBycategoryAsync(string category)
        {
            try
            {
                var results = await _elasticSearchService.ElasticSearchNewsBycategoryAsync(category);

                return Ok(results.Documents.ToList());
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

        [Authorize]
        [HttpGet("search-news-by-num")]
        public async Task<IActionResult> SearchNewsByUrlNumAsync(string num)
        {
            var result = await _elasticSearchService.ElasticSearchNewsByUrlNumAsync(num);

            try
            {
                if (result.Documents.ToList().Count > 0)
                {
                    return Ok(result.Documents.ToList());
                }

                return BadRequest("No query result");
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

        [Authorize]
        [HttpGet("recommend-news")]
        public async Task<IActionResult> RecommendNewsAsync()
        {
            try
            {
                var recommendedNews = await _elasticSearchService.ElasticSearchRecommendNewsAsync();

                return Ok(recommendedNews);
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
