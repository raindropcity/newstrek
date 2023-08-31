using newstrek.Configurations;
using Microsoft.Extensions.Options;

namespace newstrek.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly OpenAiConfig _openAiConfig;

        public OpenAiService (IOptionsMonitor<OpenAiConfig> optionsMonitor)
        {
            // inject OpenAi key to the constructor
            _openAiConfig = optionsMonitor.CurrentValue;
        }

        public async Task<string> CompleteSentence(string text)
        {
            // access the OpenAi key
            var api = new OpenAI_API.OpenAIAPI(_openAiConfig.Key);
            // get the OpenAi response
            var result = await api.Completions.GetCompletion(text);

            return result;
        }
    }
}
