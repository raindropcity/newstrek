using AngleSharp;
using newstrek.Data;

namespace newstrek.Services
{
    public class VocabularyService
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly RedisCacheManager _redisCacheManager;

        public VocabularyService (NewsTrekDbContext newsTrekDbContext, RedisCacheManager redisCacheManager)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _redisCacheManager = redisCacheManager;
        }

        // Crawler the vocabulary HTML structure
        public async Task<string> VocabularyCrawlerAsync(string key, string crawlerAddress, string className)
        {
            var cacheKey = key;
            // Check if the result is cached
            string? htmlStructure = await _redisCacheManager.GetStringAsync(cacheKey);

            if (htmlStructure == null)
            {
                var config = Configuration.Default.WithDefaultLoader();
                var address = crawlerAddress;
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);

                var def = document.QuerySelectorAll(className);
                htmlStructure = "";

                foreach (var item in def)
                {
                    htmlStructure += item.OuterHtml;
                }

                // Store the result in cache
                await _redisCacheManager.SetStringAsync(cacheKey, htmlStructure);
            }

            return htmlStructure;
        }
    }
}
