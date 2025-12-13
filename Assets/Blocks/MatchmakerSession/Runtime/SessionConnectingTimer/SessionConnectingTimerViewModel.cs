using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.Sessions.Matchmaker
{
    /// <summary>
    /// Keeps track of the time spent connecting to a session
    /// </summary>
    public class SessionConnectingTimerViewModel : IDisposable, IDataSourceViewHashProvider, INotifyBindablePropertyChanged
    {
        const string k_DefaultConnectingText = "Awaiting session connection attempt...";

        SessionObserver m_SessionObserver;
        TimeSpan m_TimeSpan;
        CancellationTokenSource m_TimerUpdateCancellationToken;

        [CreateProperty]
        public string DisplayText
        {
            get => m_Text;
            set
            {
                if (m_Text == value)
                {
                    return;
                }
                m_Text = value;
                Notify();
            }
        }
        string m_Text = k_DefaultConnectingText;

        public SessionConnectingTimerViewModel(string sessionType)
        {
            m_SessionObserver = new SessionObserver(sessionType);
            m_SessionObserver.SessionAdded += OnSessionAdded;
            m_SessionObserver.AddingSessionStarted += OnAddingSessionStarted;
            m_SessionObserver.AddingSessionFailed += OnAddingSessionFailed;
        }

        async Awaitable UpdateTimerAsync(CancellationToken cancellationToken)
        {
            float startTime = Time.realtimeSinceStartup;
            try
            {
                while (true)
                {
                    await Awaitable.EndOfFrameAsync(cancellationToken);
                    UpdateTimer(Time.realtimeSinceStartup - startTime);
                }
            }
            catch (Exception)
            {
                // Ignoring exceptions here, as the only purpose of this method is to update the timer
                // until the cancellation token gets cancelled.
            }
        }

        void UpdateTimer(float timeSinceTimerStarted)
        {
            m_TimeSpan = TimeSpan.FromSeconds(timeSinceTimerStarted);
            DisplayText = $"Time connecting: {m_TimeSpan.Minutes:D2}:{m_TimeSpan.Seconds:D2}";
        }

        void StopTimerTask()
        {
            m_TimerUpdateCancellationToken?.Cancel();
            m_TimerUpdateCancellationToken = null;
        }

        void OnSessionAdded(ISession _)
        {
            DisplayText = k_DefaultConnectingText;
            StopTimerTask();
        }

        void OnAddingSessionStarted(AddingSessionOptions options)
        {
            StopTimerTask();

            UpdateTimer(0);
            m_TimerUpdateCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            _ = UpdateTimerAsync(m_TimerUpdateCancellationToken.Token);
        }

        void OnAddingSessionFailed(AddingSessionOptions addingSessionOptions, SessionException sessionException)
        {
            DisplayText = k_DefaultConnectingText;
            StopTimerTask();
        }

        public void Dispose()
        {
            StopTimerTask();
            if (m_SessionObserver != null)
            {
                m_SessionObserver.Dispose();
                m_SessionObserver = null;
            }
        }

        /// <summary>
        /// This method is used by UIToolkit to determine if any data bound to the UI has changed.
        /// Instead of hashing the data, a text is updated when changes occur.
        /// </summary>
        public long GetViewHashCode() => HashCode.Combine(DisplayText);

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
