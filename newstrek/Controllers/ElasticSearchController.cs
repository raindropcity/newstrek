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

namespace newstrek.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        [HttpGet("search-news")] // 要分查詢權重
        public async Task<IActionResult> SearchNews(string keyword)
        {
            var results = await _elasticClient.SearchAsync<News>(
                s => s.Query(
                    q => q.QueryString(
                        d => d.Query('*' + keyword + '*')
                    )
                ).Size(18) // give me the first 18 results
            );

            if (results.Documents.ToList().Count > 0)
            {
                return Ok(new { response = "Query successfully", result = results.Documents.ToList() });
            }

            return Ok(new { response = "No query results, please check your keyword" });
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
                ).Size(18) // give me the first 18 results
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
                var userIdentity = HttpContext.User.Identity as ClaimsIdentity;
                var emailClaim = userIdentity.FindFirst(ClaimTypes.Email)?.Value;

                if (emailClaim != null)
                {
                    // Use the extracted Email to query the specific user's InterestedTopic
                    var userInterestedTopic = await _newsTrekDbContext.Users.Where(u => u.Email == emailClaim).Select(s => s.InterestedTopic).ToListAsync();

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

                    return Ok(recommendedNews);
                }

                return BadRequest("Email claim is missing or invalid");
            }

            return BadRequest("Error in Authorization of request header");
        }

        private async Task<List<News>> QueryRecommendedNews(List<string> selectedInterestedTopic)
        {
            List<ISearchResponse<News>> result = new List<ISearchResponse<News>>();
            foreach (var item in selectedInterestedTopic)
            {
                Console.WriteLine(item);
            }

            if (selectedInterestedTopic.Count > 0)
            {
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
                        .Size(100)
                    );

                    result.Add(searchResponse);
                }
            }
            else
            {
                List<string> allTopic = new List<string>()
                {
                    "world", "business", "politics", "health", "climate", "tech", "entertainment", "science", "history", "sports"
                };
                foreach (var item in allTopic)
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
                        .Size(100)
                    );

                    result.Add(searchResponse);
                }
            }

            List<News> allDocuments = result.SelectMany(response => response.Documents).ToList();

            // Random 10 篇
            Random random = new Random();
            int numberOfRandomElements = 10;
            List<News> randomNewsList = new List<News>();

            while (randomNewsList.Count < numberOfRandomElements && allDocuments.Count > 0)
            {
                int randomIndex = random.Next(0, allDocuments.Count);
                News randomNews = allDocuments[randomIndex];

                randomNewsList.Add(randomNews);
                allDocuments.RemoveAt(randomIndex); // Remove the selected news to avoid duplicates
            }

            return randomNewsList;
        }
    }
}
