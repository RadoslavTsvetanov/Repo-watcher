namespace GitManagerApp.AI;

public sealed class DummyAIService : IAIService
{
    public string Summarize(string diff) => "Automated update: summarized changes";
}
