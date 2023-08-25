using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;

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
    }
}
