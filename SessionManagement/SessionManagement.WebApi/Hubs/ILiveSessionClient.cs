namespace SessionManagement.WebApi.Hubs;

public interface ILiveSessionClient
{
    Task ReceiveSessionStateChanged(object payload);
    Task ReceiveScoreUpdate(object payload);
    Task ReceiveManualPenalty(object payload);
    Task ReceiveHintReleased(object payload);
    Task ReceiveSupportMessage(object payload);
    Task JoinRequestReceived(object payload);
    Task JoinRequestResolved(object payload);
    Task TeamProgressUpdated(object payload);
    Task TriviaAnswerSubmitted(object payload);
    Task HuntLocationReached(object payload);
    Task TeamDisconnected(object payload);
}
