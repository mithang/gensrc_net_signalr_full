namespace SignalRServer
{
    public interface ILearningHubClient
    {
        Task ReceiveMessage(string message);
        Task GetMessage(string message, string user);
    }
}