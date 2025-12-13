using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Services.Multiplayer;

namespace Blocks.Sessions.Matchmaker
{
    public static class SessionCancellationUtils
    {
        struct CancellationObserver : IDisposable
        {
            public SessionObserver SessionObserver;
            public CancellationTokenSource CancellationTokenSource;

            public void Dispose()
            {
                SessionObserver?.Dispose();
                CancellationTokenSource?.Dispose();
            }
        }

        static Dictionary<string, CancellationObserver> s_CancellationTokens = new Dictionary<string, CancellationObserver>();

        public static void RegisterCancellationToken(string sessionType, CancellationTokenSource cancellationTokenSource)
        {
            var cancellationObserver = new CancellationObserver()
            {
                SessionObserver = new SessionObserver(sessionType),
                CancellationTokenSource = cancellationTokenSource
            };

            cancellationObserver.SessionObserver.AddingSessionFailed += OnAddingSessionFailed;
            cancellationObserver.SessionObserver.SessionAdded += OnSessionAdded;

            s_CancellationTokens.Add(sessionType, cancellationObserver);
        }

        public static bool HasCancellationTokenForSessionType(string sessionType) => s_CancellationTokens.ContainsKey(sessionType);

        static void UnregisterToken(string sessionType)
        {
            if (s_CancellationTokens.TryGetValue(sessionType, out var cancellationObserver))
            {
                cancellationObserver.Dispose();
            }
            s_CancellationTokens.Remove(sessionType);
        }

        static void OnAddingSessionFailed(AddingSessionOptions addingSessionOptions, SessionException _)
        {
            UnregisterToken(addingSessionOptions.Type);
        }

        static void OnSessionAdded(ISession newSession)
        {
            UnregisterToken(newSession.Type);
        }

        public static void CancelCancellationToken(string sessionType)
        {
            if (s_CancellationTokens.TryGetValue(sessionType, out var cancellationObserver))
            {
                if (cancellationObserver.CancellationTokenSource != null &&
                    !cancellationObserver.CancellationTokenSource.IsCancellationRequested)
                {
                    cancellationObserver.CancellationTokenSource.Cancel();
                }
            }
        }
    }
}
