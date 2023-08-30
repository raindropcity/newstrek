﻿using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text.Json;

namespace newstrek.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ElasticSearchController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IElasticClient _elasticClient;
        public ElasticSearchController(IElasticClient elasticClient, NewsTrekDbContext newsTrekDbContext)
        {
            _elasticClient = elasticClient;
            _newsTrekDbContext = newsTrekDbContext;
        }

        [Authorize]
        [HttpGet("SearchNews")] // 要分查詢權重
        public async Task<IActionResult> SearchNews(string keyword)
        {
            var results = await _elasticClient.SearchAsync<News>(
                s => s.Query(
                    q => q.QueryString(
                        d => d.Query('*' + keyword + '*')
                    )
                ).Size(10) // give me the first 10 results
            );

            return Ok(results.Documents.ToList());
        }

        [Authorize]
        [HttpGet("search-news-by-category")]
        public async Task<IActionResult> SearchNewsBycategory(string category)
        {
            Console.WriteLine(category);
            var results = await _elasticClient.SearchAsync<News>(
                s => s.Query(
                    q => q.MultiMatch(m => m
                        .Fields(f => f
                            .Field(n => n.Category, boost: 2) // Boost the "category" field
                            .Field(n => n.Tag, boost: 1)      // Boost the "tag" field
                        )
                        .Query(category)
                    )
                ).Size(10) // give me the first 10 results
            );

            return Ok(results.Documents.ToList());
        }

        [Authorize]
        [HttpGet("search-news-by-num")]
        public async Task<IActionResult> SearchNewsByUrlNum(string num)
        {
            Console.WriteLine(num);
            // URL是keyword，不能用Term
            var result = await _elasticClient.SearchAsync<News>(
                        s => s.Query(
                        q => q.Wildcard(w => w
                                .Field(f => f.URL)
                                .Value('*' + num + '*')
                            )
                        )
                    );

            foreach (var item in result.Documents.ToList())
            {
                Console.WriteLine(item);
            }

            return Ok(result.Documents.ToList());
        }

        [HttpPost("AddNewsToElasticSearch")]
        public async Task<IActionResult> AddNews()
        {
            // Clear the "news" index in ElasticSearch first
            var response = await _elasticClient.DeleteByQueryAsync<News>(d => d
                .Index("news")
                .Query(q => q.MatchAll())
            );

            if (!response.IsValid)
            {
                // Handle error
                Console.WriteLine("Error deleting documents from Elasticsearch");
            }
            else
            {
                Console.WriteLine($"Deleted {response.Deleted} documents from Elasticsearch");
            }

            // Add the data from SQL Server to the "news" index in ElasticSearch
            var newsFromSql = await _newsTrekDbContext.News.ToListAsync(); // Fetch news from SQL Server

            foreach (var news in newsFromSql)
            {
                var indexResponse = await _elasticClient.IndexDocumentAsync(news);
                if (!indexResponse.IsValid)
                {
                    Console.WriteLine("something wrong about Transfer News To Elasticsearch");
                }
            }

            return Ok("Add news successfully");
        }

        [Authorize]
        [HttpGet("recommend-news")]
        public async Task<IActionResult> RecommendNews()
        {
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            if (authorizationHeader != null && authorizationHeader.StartsWith("Bearer "))
            {
                // extracts the JWT token from the header by removing the first 7 characters ("Bearer ")
                string jwtToken = authorizationHeader.Substring(7);

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtTokenObject = tokenHandler.ReadJwtToken(jwtToken);

                // Extract the Email involved in JWT
                var emailClaim = jwtTokenObject.Claims.Where(c => c.Type.Contains("emailaddress")).Select(s => s.Value).ToList();

                if (emailClaim != null)
                {
                    // Use the extracted Email to query the specific user's InterestedTopic
                    var userInterestedTopic = await _newsTrekDbContext.Users.Where(u => u.Email == emailClaim[0]).Select(s => s.InterestedTopic).ToListAsync();

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

                    var recommendedNews = await QueryRecommendedNews(selectedInterestedTopic);

                    return Ok(recommendedNews[0].Documents.ToList());
                }

                return BadRequest("Email claim is missing or invalid");
            }

            return BadRequest("Error in Authorization of request header");
        }

        private async Task<List<ISearchResponse<News>>> QueryRecommendedNews(List<string> selectedInterestedTopic)
        {
            List<ISearchResponse<News>> result = new List<ISearchResponse<News>>();
            int qty = (int)Math.Ceiling(10.0 / selectedInterestedTopic.Count);

            foreach (var item in selectedInterestedTopic)
            {
                var searchResponse = await _elasticClient.SearchAsync<News>(s => s
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Fields(f => f
                                .Field(fld => fld.Category, boost: 2)
                                .Field(fld => fld.Article, boost: 1.5)
                                .Field(fld => fld.Title)
                            )
                            .Query(item)
                        )
                    )
                    .Size(qty)
                );

                result.Add(searchResponse);
            }

            return result;
        }
    }
}
