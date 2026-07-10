namespace DirectorPrompt.Domain.Services;

public static class RoundContext
{
    private static readonly AsyncLocal<long?> currentRoundID = new();

    private static readonly AsyncLocal<long?> currentSessionID = new();

    public static long? Current => currentRoundID.Value;

    public static long? SessionID => currentSessionID.Value;

    public static IDisposable Enter(long sessionID, long roundID)
    {
        var previousRound   = currentRoundID.Value;
        var previousSession = currentSessionID.Value;

        currentRoundID.Value   = roundID;
        currentSessionID.Value = sessionID;

        return new Scope
        (() =>
            {
                currentRoundID.Value   = previousRound;
                currentSessionID.Value = previousSession;
            }
        );
    }

    private sealed class Scope
    (
        Action onDispose
    ) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
