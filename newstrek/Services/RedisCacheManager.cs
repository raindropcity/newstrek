using StackExchange.Redis;

namespace newstrek.Services
{
    public class RedisCacheManager
    {
        private readonly IDatabase _redisDatabase;

        public RedisCacheManager(IConfiguration configuration)
        {
            var primaryEndPoint = configuration["Redis:PrimaryEndPoint"];
            var readerEndPoint = configuration["Redis:ReaderEndPoint"];
            var password = configuration["Redis:Password"];
            var options = ConfigurationOptions.Parse($"{primaryEndPoint},{readerEndPoint}");
            options.Password = password;
            options.Ssl = true;
            options.AllowAdmin = true; // 如果需要進行管理操作，可以設置為 true
            options.AbortOnConnectFail = false; // 如果要允許重試連接，可以設置為 false
            
            var redisConnection = ConnectionMultiplexer.Connect(options);
            _redisDatabase = redisConnection.GetDatabase();
        }

        public async Task<string?> GetStringAsync(string key)
        {
            return await _redisDatabase.StringGetAsync(key);
        }

        public async Task SetStringAsync(string key, string value, TimeSpan? expiration = null)
        {
            await _redisDatabase.StringSetAsync(key, value, expiration);
        }
    }
}
