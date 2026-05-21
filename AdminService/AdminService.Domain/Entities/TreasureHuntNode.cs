namespace AdminService.Domain.Entities;

public class TreasureHuntNode : MissionNode
{
    public string LocationInstructions { get; private set; }
    public string SecretQrCode { get; private set; }
    public TimeSpan BaseTime { get; private set; }

    private readonly List<Hint> _hints = [];
    public IReadOnlyCollection<Hint> Hints => _hints.AsReadOnly();

    private TreasureHuntNode(Guid missionId, string title, int order, string instructions, string qrCode, TimeSpan baseTime)
    {
        Id = Guid.NewGuid();
        MissionId = missionId;
        Title = title;
        Order = order;
        LocationInstructions = instructions;
        SecretQrCode = qrCode;
        BaseTime = baseTime;
    }

    public static TreasureHuntNode Create(Guid missionId, string title, int order, string instructions, string qrCode, TimeSpan baseTime)
    {
        if (string.IsNullOrWhiteSpace(qrCode))
            throw new ArgumentException("El código QR secreto es obligatorio.");

        return new TreasureHuntNode(missionId, title, order, instructions, qrCode, baseTime);
    }

    public void AddHint(string content)
    {
        _hints.Add(new Hint(Guid.NewGuid(), Id, content));
    }

    public override TimeSpan CalculateEstimatedTime() => BaseTime;
}