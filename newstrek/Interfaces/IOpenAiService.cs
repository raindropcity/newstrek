namespace newstrek.Interfaces
{
    public interface IOpenAiService
    {
        Task<string> CompleteSentence(string text);
    }
}
