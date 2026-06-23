namespace Topiqueue.Core.BackgroundService;

public interface ITpqBackgroundService
{
    void StartBackgroundService();
    void SendStopSignal();
}