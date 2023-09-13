namespace newstrek.Interfaces
{
    public interface IOpenAiService
    {
        Task<string> CompleteSentence(string text);

        Task<string> MakeSentenceAsync(HashSet<string> vocabularyList);
    }
}
