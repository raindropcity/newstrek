namespace newstrek.Services
{
    /* The BackgroundService class is designed to help you create long-running background tasks or services in your application. It implements the IHostedService interface and provides a convenient way to define the logic for your background service */
    public class NewsCrawlerTimerService : BackgroundService
    {
        private readonly NewsCrawlerService _newsCrawlerService;

        public NewsCrawlerTimerService(NewsCrawlerService newsCrawlerService)
        {
            _newsCrawlerService = newsCrawlerService;
        }

        /* The BackgroundService class is designed to help you create long-running background tasks or services in your application. It implements the IHostedService interface and provides a convenient way to define the logic for your background service. */
        /* The ExecuteAsync method is where you put the actual implementation of the background task. It's the method that will be executed by the ASP.NET Core framework when the background service starts. You override this method in your own derived class (such as the NewsCrawlerService in the example I provided) to define the specific behavior of your background service. */
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("News crawler service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get the current time and check if it's the desired time to crawl news
                //// Create a DateTime in UTC
                //DateTime utcDateTime = DateTime.UtcNow;
                //// Taiwan time zone
                //TimeZoneInfo taiwanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
                //// Convert to Taiwan time
                //DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, taiwanTimeZone);
                var currentTime = DateTime.UtcNow;
                // 預設是英國時間，慢台灣時區8小時，因此凌晨3點往前推8小時為19點
                var desiredTime = new TimeSpan(19, 0, 0);
                // 每天台灣時間清晨3:00 - 3:59 project有run的話，啟動爬蟲新聞
                if (currentTime.TimeOfDay >= desiredTime && currentTime.TimeOfDay < desiredTime.Add(TimeSpan.FromHours(1)))
                {
                    // Execute the news crawling task here
                    Console.WriteLine("Crawling news...");

                    _newsCrawlerService.NewsCrawlerAsync();
                }
                // Wait for a period before checking again (e.g., every hour)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

            Console.WriteLine("News crawler service is stopping.");
        }
    }
}
