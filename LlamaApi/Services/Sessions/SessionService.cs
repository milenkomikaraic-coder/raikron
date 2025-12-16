using LlamaApi.Core.Domain;

namespace LlamaApi.Services.Sessions;

public class SessionService
{
    private readonly Dictionary<string, SessionData> _sessions = new();

    public Task ResetSessionAsync(string sessionId)
    {
        _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }

    public Task<SessionData> GetOrCreateSessionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            session = new SessionData { SessionId = sessionId };
            _sessions[sessionId] = session;
        }
        return Task.FromResult(session);
    }
}
