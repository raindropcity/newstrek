using Microsoft.EntityFrameworkCore;
using Nest;
using newstrek.Data;
using newstrek.Models;

namespace newstrek.Services
{
    public class ElasticSearchService
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IElasticClient _elasticClient;
        private readonly JwtParseService _jwtParseService;
        private readonly MapObjectToListService _mapObjectToListService;

        public ElasticSearchService (NewsTrekDbContext newsTrekDbContext, IElasticClient elasticClient, JwtParseService jwtParseService, MapObjectToListService mapObjectToListService)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _elasticClient = elasticClient;
            _jwtParseService = jwtParseService;
            _mapObjectToListService = mapObjectToListService;
        }

        public async Task<ISearchResponse<News>> ElasticSearchNewsAsync (string keyword)
        {
            var results = await _elasticClient.SearchAsync<News>(
                s => s.Query(
                    q => q.QueryString(
                        d => d.Query('*' + keyword + '*')
                    )
                ).Size(18) // give me the first 18 results
            );

            return results;
        }

        public async Task<ISearchResponse<News>> ElasticSearchNewsBycategoryAsync(string category)
        {
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

            return results;
        }

        public async Task<ISearchResponse<News>> ElasticSearchNewsByUrlNumAsync(string num)
        {
            // URL是keyword，不能用Term
            var result = await _elasticClient.SearchAsync<News>(
                        s => s.Query(
                        q => q.Wildcard(w => w
                                .Field(f => f.URL)
                                .Value('*' + num + '*')
                            )
                        )
                    );

            return result;
        }

        public async Task<List<News>> ElasticSearchRecommendNewsAsync()
        {
            var email = _jwtParseService.ParseEmail();

            // Use the extracted Email to query the specific user's InterestedTopic
            var userInterestedTopic = await _newsTrekDbContext.Users.Where(u => u.Email == email).Select(s => s.InterestedTopic).ToListAsync();

            List<string> selectedInterestedTopic = _mapObjectToListService.MapInterestedTopicToListAsync(userInterestedTopic);

            var recommendedNews = await QueryRecommendedNewsAsync(selectedInterestedTopic);

            return recommendedNews;
        }

        private async Task<List<News>> QueryRecommendedNewsAsync(List<string> selectedInterestedTopic)
        {
            List<ISearchResponse<News>> result = new List<ISearchResponse<News>>();

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
            { // 使用者沒有選interested topic的話，以全部類別為基礎推薦新聞
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
