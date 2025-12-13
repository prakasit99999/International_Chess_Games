using System;
using System.Threading;
using Blocks.Common;
using Blocks.Sessions.Common;
using Unity.Properties;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UIElements;

namespace Blocks.Sessions.Matchmaker
{
    /// <summary>
    /// A button to start a new matchmaker session
    /// </summary>
    [UxmlElement]
    public partial class CreateMatchmakerSessionButton : Button
    {
        const string k_MatchmakeButtonText = "MATCHMAKE";

        [UxmlAttribute, CreateProperty]
        public SessionSettings SessionSettings
        {
            get => m_SessionSettings;
            set
            {
                if (m_SessionSettings == value)
                {
                    return;
                }

                m_SessionSettings = value;
                if (panel != null)
                {
                    UpdateBindings();
                }
            }
        }
        SessionSettings m_SessionSettings;

        [UxmlAttribute]
        public MatchmakerQueueAsset MatchmakerSettings;

        DataBinding m_DataBinding;
        CreateMatchmakerSessionViewModel m_ViewModel;

        public CreateMatchmakerSessionButton()
        {
            text = k_MatchmakeButtonText;

            AddToClassList(BlocksTheme.Button);
            m_DataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CreateMatchmakerSessionViewModel.CanCreateSession)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(enabledSelf)), m_DataBinding);
            clicked += CreateMatchmakerSession;

            RegisterCallback<AttachToPanelEvent>(_ => UpdateBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => CleanupBindings());
        }

        void CreateMatchmakerSession()
        {
            if (!SessionSettings)
            {
                Debug.LogError("SessionSettings is null, it needs to be assigned in the uxml.");
                return;
            }
            var matchmakerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(UnityEngine.Application.exitCancellationToken);
            SessionCancellationUtils.RegisterCancellationToken(SessionSettings.sessionType, matchmakerCancellationSource);

            if (!MatchmakerSettings)
            {
                Debug.LogError("MatchmakerSettings is null, it needs to be assigned in the uxml.");
                return;
            }
            var matchmakerOptions = new MatchmakerOptions { QueueName = MatchmakerSettings.Name };
            _ = m_ViewModel.MatchmakeSessionAsync(matchmakerOptions, SessionSettings.ToSessionOptions(), matchmakerCancellationSource.Token);
        }

        void UpdateBindings()
        {
            CleanupBindings();
            m_ViewModel = new CreateMatchmakerSessionViewModel(SessionSettings?.sessionType);
            m_DataBinding.dataSource = m_ViewModel;
        }

        void CleanupBindings()
        {
            if (m_DataBinding.dataSource is IDisposable disposable)
            {
                disposable.Dispose();
            }

            m_ViewModel = null;
            m_DataBinding.dataSource = null;
        }
    }
}
