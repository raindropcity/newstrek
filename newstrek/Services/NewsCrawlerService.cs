using AngleSharp;
using newstrek.Data;
using newstrek.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.Text;

namespace newstrek.Services
{
    public class NewsCrawlerService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IElasticClient _elasticClient;
        public NewsCrawlerService(IElasticClient elasticClient, IServiceScopeFactory scopeFactory)
        {
            _elasticClient = elasticClient;
            _scopeFactory = scopeFactory;
        }
        
        // launch the crawler to get news and save them to the SQL server
        public async void NewsCrawlerAsync()
        {
            /* 由於NewsTrekDbContext在Program.cs註冊的生命週期是AddScoped，
             但NewsCrawlerTimerService.cs是background service，它是以AddHostedService來註冊(屬於singleton)
            ，會衝突。  因此這邊用using創一個scope來注入NewsTrekDbContext */
            using (var scope = _scopeFactory.CreateScope())
            {
                Console.WriteLine("Start crawling news...");

                var _newsTrekDbContext = scope.ServiceProvider.GetRequiredService<NewsTrekDbContext>();

                // 決定總共要儲存幾篇新聞
                int totalNews = 1000;

                // 目前資料庫(ElasticSearch)中有幾篇新聞
                var countResponse = _elasticClient.Count<News>(c => c
                    .Index("news") // Specify the index name
                );
                long countNews = countResponse.Count;

                // 本次爬蟲中，每個類別要爬幾篇新聞(有10個類別)
                int qtyOfPerCategory = (int)((totalNews - countNews) / 10);

                if (totalNews <= countNews)
                {
                    Console.WriteLine("News is sufficient in DB. No need to launch crawler.");
                }
                Console.WriteLine(qtyOfPerCategory);

                try
                {
                    HashSet<string> worldNewsUrls = await CrawlTimesNewsUrlsAsync("world", qtyOfPerCategory);
                    HashSet<string> businessNewsUrls = await CrawlTimesNewsUrlsAsync("business", qtyOfPerCategory);
                    HashSet<string> politicsNewsUrls = await CrawlTimesNewsUrlsAsync("politics", qtyOfPerCategory);
                    HashSet<string> healthNewsUrls = await CrawlTimesNewsUrlsAsync("health", qtyOfPerCategory);
                    HashSet<string> climateNewsUrls = await CrawlTimesNewsUrlsAsync("climate", qtyOfPerCategory);
                    HashSet<string> techNewsUrls = await CrawlTimesNewsUrlsAsync("tech", qtyOfPerCategory);
                    HashSet<string> entertainmentNewsUrls = await CrawlTimesNewsUrlsAsync("entertainment", qtyOfPerCategory);
                    HashSet<string> scienceNewsUrls = await CrawlTimesNewsUrlsAsync("science", qtyOfPerCategory);
                    HashSet<string> historyNewsUrls = await CrawlTimesNewsUrlsAsync("history", qtyOfPerCategory);
                    HashSet<string> sportsNewsUrls = await CrawlTimesNewsUrlsAsync("sports", qtyOfPerCategory);

                    HashSet<string> allNewsUrls = new HashSet<string>(worldNewsUrls);
                    allNewsUrls.UnionWith(businessNewsUrls);
                    allNewsUrls.UnionWith(politicsNewsUrls);
                    allNewsUrls.UnionWith(healthNewsUrls);
                    allNewsUrls.UnionWith(climateNewsUrls);
                    allNewsUrls.UnionWith(techNewsUrls);
                    allNewsUrls.UnionWith(entertainmentNewsUrls);
                    allNewsUrls.UnionWith(scienceNewsUrls);
                    allNewsUrls.UnionWith(historyNewsUrls);
                    allNewsUrls.UnionWith(sportsNewsUrls);

                    List<News> news = await CrawlTimesNewsDetailAsync(allNewsUrls);

                    await _newsTrekDbContext.News.AddRangeAsync(news);

                    await _newsTrekDbContext.SaveChangesAsync();

                    Console.WriteLine("News crawler completed, latest news are saved into database.");

                    Console.WriteLine("Start adding news from database to ElasticSearch.");
                    AddNewsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        // Add news from SQL server to ElasticSearch
        private async void AddNewsAsync()
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _newsTrekDbContext = scope.ServiceProvider.GetRequiredService<NewsTrekDbContext>();

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

                try
                {
                    // Add the data from SQL Server to the "news" index in ElasticSearch
                    var newsFromSql = await _newsTrekDbContext.News.ToListAsync(); // Fetch news from SQL Server

                    foreach (var news in newsFromSql)
                    {
                        var indexResponse = await _elasticClient.IndexDocumentAsync(news);
                        if (!indexResponse.IsValid)
                        {
                            Console.WriteLine("something wrong about adding News To Elasticsearch");
                        }
                    }

                    Console.WriteLine("Add news from SQL server to ElasticSearch successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task<HashSet<string>> CrawlTimesNewsUrlsAsync(string? category, int qtyOfPerCategory)
        {
            HashSet<string> newsUrls = new HashSet<string>();

            int page = 1;

            // specify the quantity for one category
            while (newsUrls.Count() < qtyOfPerCategory)
            {
                var config = Configuration.Default.WithDefaultLoader();
                var address = (page == 1) ? $"https://time.com/section/{category}/" : $"https://time.com/section/{category}/?page={page}";
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);

                Console.WriteLine($"crawling the {category} news URL from {address}");

                var linkElements = document.QuerySelectorAll(".section-related .taxonomy-tout a");
                foreach (var linkElement in linkElements)
                {
                    string? newsUrl = linkElement.GetAttribute("href");

                    var searchResponse = await _elasticClient.SearchAsync<News>(
                        s => s.Query(
                            q => q.Term(
                                t => t.Field(f => f.URL).Value($"https://time.com{newsUrl}")
                            )
                        )
                    );
                    bool TheUrlIsAlreadyExistInElasticSearch = searchResponse.Hits.Any();
                    Console.WriteLine(TheUrlIsAlreadyExistInElasticSearch);
                    // 判斷這次爬蟲有沒有重複的URL(同一篇新聞可能在不同類別的頁面出現)，以及是否已經在之前的爬蟲被爬過(用ElasticSearch來查)，都通過的話才能進入本次要加入資料庫的HashSet中
                    if (!newsUrls.Contains(newsUrl) && !TheUrlIsAlreadyExistInElasticSearch)
                    {
                        newsUrls.Add(newsUrl);
                    }
                }

                await Task.Delay(1000);

                page++;
            }

            return newsUrls;
        }

        private async Task<List<News>> CrawlTimesNewsDetailAsync(HashSet<string> allNewsUrls)
        {
            Console.WriteLine("starting crawling news detail");

            List<News> newsList = new List<News>();

            foreach (var url in allNewsUrls)
            {
                var config = Configuration.Default.WithDefaultLoader();
                var address = $"https://time.com{url}";
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);

                var URL = $"https://time.com{url}";
                var date = document.QuerySelector(".timestamp time")?.GetAttribute("datetime");
                var title = document.QuerySelector(".article-top .headline")?.TextContent;
                var category = document.QuerySelector("head meta[property=\"category\"]")?.GetAttribute("content");
                var tag = document.QuerySelector("head meta[property=\"primary_tag\"]")?.GetAttribute("content");
                /* Use StringBuilder class to concatenate strings. The "+=" operator can do the same task.
                However, every time the new string created in C#, it has to occupy some space of memory.
                That's why it does not efficient to use "+=" to concatenate many strings. */
                var articleBuilder = new StringBuilder();
                foreach (var p in document.QuerySelectorAll(".padded p"))
                {
                    articleBuilder.Append($"<p>{p.TextContent}</p><br>");
                }
                var article = articleBuilder.ToString();

                if (date != null && title != null && article != null)
                {
                    newsList.Add(new News() { URL = URL, Date = date, Title = title, Article = article, Category = category, Tag = tag });
                }
            }

            return newsList;
        }
    }
}
