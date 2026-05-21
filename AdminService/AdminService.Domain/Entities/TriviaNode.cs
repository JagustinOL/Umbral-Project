namespace AdminService.Domain.Entities;

public class TriviaNode : MissionNode
{
    public string Question { get; private set; }
    public string CorrectAnswer { get; private set; }

    public List <string> IncorrectAnswers { get; private set;}
    public TimeSpan BaseTime { get; private set; }

    private TriviaNode(Guid missionId, string title, int order, string question, string correctAnswer, List <string> incorrectAnswers, TimeSpan baseTime)
    {
        Id = Guid.NewGuid();
        MissionId = missionId;
        Title = title;
        Order = order;
        Question = question;
        CorrectAnswer = correctAnswer;
        IncorrectAnswers = incorrectAnswers;
        BaseTime = baseTime;
    }

    public static TriviaNode Create(Guid missionId, string title, int order, string question, string correctAnswer, List<string> incorrectAnswers, TimeSpan baseTime)
    {
        if (string.IsNullOrWhiteSpace(correctAnswer))
            throw new ArgumentException("La respuesta correcta es obligatoria.");

        return new TriviaNode(missionId, title, order, question, correctAnswer, incorrectAnswers, baseTime);
    }

    public override TimeSpan CalculateEstimatedTime() => BaseTime;
}