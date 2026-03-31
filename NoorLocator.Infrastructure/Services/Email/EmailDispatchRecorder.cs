using System.Collections.Concurrent;

namespace NoorLocator.Infrastructure.Services.Email;

public class EmailDispatchRecorder
{
    private readonly ConcurrentQueue<EmailDispatchMessage> messages = new();

    public void Record(EmailDispatchMessage message)
    {
        messages.Enqueue(message);

        while (messages.Count > 200 && messages.TryDequeue(out _))
        {
        }
    }

    public IReadOnlyCollection<EmailDispatchMessage> Snapshot() => messages.ToArray();

    public void Clear()
    {
        while (messages.TryDequeue(out _))
        {
        }
    }
}
