using newstrek.Configurations;
using Microsoft.Extensions.Options;
using newstrek.Interfaces;
using OpenAI_API.Completions;

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
            CompletionRequest completionRequest = new CompletionRequest 
            { 
                Prompt = text,
                MaxTokens = 30,
            };
            var result = await api.Completions.CreateAndFormatCompletion(completionRequest);

            result = result.Replace(text, "").Trim();

            return result;
        }

        public async Task<string> MakeSentenceAsync (HashSet<string> vocabularyList)
        {
            string text = string.Join(", ", vocabularyList);
            string question = $"Write a short essay within 30 to 45 words. Must involve the words: \"{text}\".";
            Console.WriteLine(question);

            // make a request to OpenAi and get the response
            var result = await CompleteSentence(question);

            return result;
        }
    }
}
