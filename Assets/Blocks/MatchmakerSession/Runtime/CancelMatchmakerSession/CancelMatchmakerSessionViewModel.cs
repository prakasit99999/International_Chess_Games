using System;
using System.Runtime.CompilerServices;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine.UIElements;

namespace Blocks.Sessions.Matchmaker
{
    class CancelMatchmakerSessionViewModel : IDisposable, IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        SessionObserver m_SessionObserver;

        [CreateProperty]
        public bool CanCancel
        {
            get => m_CanCancel;
            private set
            {
                if (m_CanCancel == value)
                {
                    return;
                }

                m_CanCancel = value;
                Notify();
            }
        }
        bool m_CanCancel;

        public CancelMatchmakerSessionViewModel(string sessionType)
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

        void OnAddingSessionStarted(AddingSessionOptions addingSessionOptions)
        {
            if (SessionCancellationUtils.HasCancellationTokenForSessionType(addingSessionOptions.Type))
                CanCancel = true;
        }

        void OnAddingSessionFailed(AddingSessionOptions addingSessionOptions, SessionException sessionException)
        {
            CanCancel = false;
        }

        void OnSessionAdded(ISession newSession)
        {
            CanCancel = false;
        }

        public void Dispose()
        {
            if (m_SessionObserver != null)
            {
                m_SessionObserver.Dispose();
                m_SessionObserver = null;
            }
        }

        /// <summary>
        /// This method is used by UIToolkit to determine if any data bound to the UI has changed.
        /// Instead of hashing the data, an m_CanCancel boolean is toggled when changes occur.
        /// </summary>
        public long GetViewHashCode() => m_CanCancel ? 1 : 0;

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
