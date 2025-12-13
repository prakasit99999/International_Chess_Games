using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine.UIElements;

namespace Blocks.Sessions.Matchmaker
{
    public class CreateMatchmakerSessionViewModel : IDisposable, IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        SessionObserver m_SessionObserver;
        ISession m_Session;

        bool m_CanCreateSession = true;
        [CreateProperty]
        public bool CanCreateSession
        {
            get => m_CanCreateSession;
            private set
            {
                if (m_CanCreateSession == value)
                {
                    return;
                }

                m_CanCreateSession = value;
                Notify();
            }
        }

        public CreateMatchmakerSessionViewModel(string sessionType)
        {
            m_SessionObserver = new SessionObserver(sessionType);
            m_SessionObserver.AddingSessionStarted += OnAddingSessionStarted;
            m_SessionObserver.AddingSessionFailed += OnAddingSessionFailed;
            m_SessionObserver.SessionAdded += OnSessionAdded;
            if (m_SessionObserver.Session != null)
            {
                OnSessionAdded(m_SessionObserver.Session);
            }
        }

        void OnAddingSessionStarted(AddingSessionOptions _)
        {
            CanCreateSession = false;
        }

        void OnAddingSessionFailed(AddingSessionOptions addingSessionOptions, SessionException sessionException)
        {
            CanCreateSession = true;
        }

        void OnSessionAdded(ISession newSession)
        {
            CanCreateSession = false;

            m_Session = newSession;
            m_Session.RemovedFromSession += OnSessionRemoved;
            m_Session.Deleted += OnSessionRemoved;
        }

        void OnSessionRemoved()
        {
            CanCreateSession = true;
            CleanupSession();
        }

        public async Task<ISession> MatchmakeSessionAsync(MatchmakerOptions matchmakerOptions, SessionOptions sessionOptions, CancellationToken cancellationToken)
        {
            return await MultiplayerService.Instance.MatchmakeSessionAsync(matchmakerOptions, sessionOptions, cancellationToken);
        }

        void CleanupSession()
        {
            m_Session.RemovedFromSession -= OnSessionRemoved;
            m_Session.Deleted -= OnSessionRemoved;
            m_Session = null;
        }

        public void Dispose()
        {
            if (m_SessionObserver != null)
            {
                m_SessionObserver.Dispose();
                m_SessionObserver = null;
            }

            if (m_Session != null)
            {
                CleanupSession();
            }
        }

        /// <summary>
        /// This method is used by UIToolkit to determine if any data bound to the UI has changed.
        /// Instead of hashing the data, an m_CanCreateSession boolean is toggled when changes occur.
        /// </summary>
        public long GetViewHashCode() => m_CanCreateSession ? 1 : 0;

        /// <summary>
        /// Suggested implementation of INotifyBindablePropertyChanged from UIToolkit.
        /// </summary>
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;
        void Notify([CallerMemberName] string property = null)
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
