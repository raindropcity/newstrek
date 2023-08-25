using AngleSharp.Dom;
using AngleSharp;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;
using AngleSharp.Html.Parser;
using newstrek.Models;
using AngleSharp.Browser.Dom;
using newstrek.Data;
using Nest;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System;

namespace newstrek.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CrawlerController : ControllerBase
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly IElasticClient _elasticClient;
        public CrawlerController(IElasticClient elasticClient, NewsTrekDbContext newsTrekDbContext)
        {
            _elasticClient = elasticClient;
            _newsTrekDbContext = newsTrekDbContext;
        }

        [HttpGet] // 啟動爬蟲，並將爬蟲結果存入DB
        public async Task<IActionResult> NewsCrawlerAsync()
        {
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
                return Ok("News is sufficient in DB. No need to launch crawler.");
            }
            Console.WriteLine(qtyOfPerCategory);

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

            return Ok(news);
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

        /*爬蟲紐約時報：棄用*/
        //private async Task<List<string>> CrawlNyNewsUrls(string? category)
        //{
        //    List<string> newsUrls = new List<string>();

        //    try
        //    {
        //        var options = new ChromeOptions();
        //        options.AddArgument("--headless"); // Run Chrome in headless mode
        //        using var driver = new ChromeDriver(options);

        //        driver.Url = $"https://www.nytimes.com/section/{category}";

        //        // Define scroll step and total scroll distance
        //        int scrollStep = 500;
        //        int totalScrollDistance = 0;

        //        int countNewsOnCurrentPage = 0;

        //        while (newsUrls.Count() < 25)
        //        {
        //            string js = $"window.scrollTo(0, {totalScrollDistance})";
        //            ((IJavaScriptExecutor)driver).ExecuteScript(js);
        //            await Task.Delay(2000);

        //            totalScrollDistance += scrollStep;

        //            // Get the updated page source after scrolling
        //            string pageSource = driver.PageSource;
        //            var parser = new HtmlParser();
        //            var document = parser.ParseDocument(pageSource);

        //            Console.WriteLine($"{document.QuerySelectorAll("#stream-panel li a").Count()}");
        //            Console.WriteLine(totalScrollDistance);

        //            if (document.QuerySelector("a span:contains('Log in')") != null)
        //            {
        //                Console.WriteLine("ZZZZZZZZZZZZZZZZZZZZZZZZZZ");
        //                var loginLink = driver.FindElement(By.CssSelector("header section :nth-child(4) :nth-child(2)"));
        //                loginLink.Click();
        //                Thread.Sleep(1000);
        //                Console.WriteLine("111111");

        //                // Handle the login process by inputting email and password
        //                var emailField = driver.FindElement(By.CssSelector("#email"));
        //                emailField.SendKeys("raindropcity0209@gmail.com");
        //                Thread.Sleep(1000);
        //                Console.WriteLine("222222");

        //                var emailSubmitBtn = driver.FindElement(By.CssSelector("button[data-testid=\"submit-email\"]"));
        //                emailSubmitBtn.Click();
        //                Thread.Sleep(1000);
        //                Console.WriteLine("333333");

        //                var passwordField = driver.FindElement(By.CssSelector("#password"));
        //                passwordField.SendKeys("raindrop0209");
        //                Thread.Sleep(1000);
        //                Console.WriteLine("444444");

        //                var loginBtn = driver.FindElement(By.CssSelector("button[data-testid=\"login-button\"]"));
        //                loginBtn.Click();
        //                Thread.Sleep(1000);
        //                Console.WriteLine("555555");

        //                var withoutSubscribeBtn = driver.FindElement(By.XPath("//a[contains(text(), 'Continue without subscribing')]"));
        //                withoutSubscribeBtn.Click();
        //                Thread.Sleep(3000);
        //                Console.WriteLine("666666");
        //            }

        //            if (document.QuerySelector("button[aria-label=\"Account Information\"]") != null)
        //            {
        //                Console.WriteLine("Login in success!!!!!!!!!!!!!!!!!!!!!!!");
        //            }

        //            // Extract news URLs using AngleSharp
        //            var linkElements = document.QuerySelectorAll("#stream-panel li a");
        //            // Add the Url to the List only when there are new urls loading on the website
        //            if (linkElements.Count() > countNewsOnCurrentPage)
        //            {
        //                countNewsOnCurrentPage = linkElements.Count();

        //                foreach (var linkElement in linkElements)
        //                {
        //                    string? newsUrl = linkElement.GetAttribute("href");
        //                    if (!newsUrls.Contains(newsUrl) && newsUrl.StartsWith("/2023"))
        //                    {
        //                        newsUrls.Add(newsUrl);
        //                    }
        //                }
        //            }
        //        }

        //        driver.Quit();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }

        //    return newsUrls;
        //}
    }
}
