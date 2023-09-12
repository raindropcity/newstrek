using AngleSharp;
using Azure;
using Microsoft.EntityFrameworkCore;
using newstrek.Data;
using newstrek.Models;
using System.Net.Http;
using System.Text.Json;

namespace newstrek.Services
{
    public class VocabularyService
    {
        private readonly NewsTrekDbContext _newsTrekDbContext;
        private readonly RedisCacheManager _redisCacheManager;
        private readonly JwtParseService _jwtParseService;
        private readonly IHttpClientFactory _httpClientFactory;

        public VocabularyService (NewsTrekDbContext newsTrekDbContext, RedisCacheManager redisCacheManager, JwtParseService jwtParseService, IHttpClientFactory httpClientFactory)
        {
            _newsTrekDbContext = newsTrekDbContext;
            _redisCacheManager = redisCacheManager;
            _jwtParseService = jwtParseService;
            _httpClientFactory = httpClientFactory;
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

        public async Task<bool> SaveVocabularyIfNotExistsAsync(string word)
        {
            var userId = _jwtParseService.ParseUserId();

            // Check if the vocabulary already exists
            bool vocabularyExists = await _newsTrekDbContext.Vocabularies
                .AnyAsync(v => v.Word == word && v.UserId == userId);

            if (!vocabularyExists)
            {
                // If it doesn't exist, add it to the database
                _newsTrekDbContext.Vocabularies.Add(new Vocabulary { Word = word, UserId = userId });
                await _newsTrekDbContext.SaveChangesAsync();

                return true; // Indicates that the vocabulary was added
            }

            return false; // Indicates that the vocabulary already exists
        }

        public async Task<bool> DeleteVocabularyAsync(string word)
        {
            var email = _jwtParseService.ParseEmail();

            var user = await _newsTrekDbContext.Users
            .Where(u => u.Email == email)
            .Include(u => u.Vocabularies)
            .FirstOrDefaultAsync();

            if (user != null)
            {
                var vocabularyToDelete = user.Vocabularies.FirstOrDefault(v => v.Word == word);

                if (vocabularyToDelete != null)
                {
                    _newsTrekDbContext.Vocabularies.Remove(vocabularyToDelete);
                    await _newsTrekDbContext.SaveChangesAsync();
                    return true; // Vocabulary was deleted
                }
            }

            return false; // Vocabulary was not found or deleted
        }

        public async Task<dynamic> LookUPWebsterDictionaryAsync(string word)
        {
            // Key for Collegiate® Dictionary with Audio
            string? DictionaryApiKey = Environment.GetEnvironmentVariable("DictionaryApiKey");
            string? DictionaryRequestUrl = $"https://www.dictionaryapi.com/api/v3/references/collegiate/json/{word}?key={DictionaryApiKey}";
            // Create an HttpClient instance using the factory
            var httpClient = _httpClientFactory.CreateClient();
            // Send the request to Webster Collegiate® Dictionary with Audio API
            var response = await httpClient.GetAsync(DictionaryRequestUrl);

            string responseBody = await response.Content.ReadAsStringAsync();

            dynamic result = JsonSerializer.Deserialize<dynamic>(responseBody);

            return result;
        }
    }
}
