namespace newstrek.Services
{
    public interface IOpenAiService
    {
        Task<string> CompleteSentence(string text);
    }
}
