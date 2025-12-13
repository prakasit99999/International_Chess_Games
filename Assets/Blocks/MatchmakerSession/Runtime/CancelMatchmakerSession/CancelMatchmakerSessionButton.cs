using System;
using Blocks.Common;
using Unity.Properties;
using UnityEngine.UIElements;

namespace Blocks.Sessions.Matchmaker
{
    [UxmlElement]
    public partial class CancelMatchmakerSessionButton : Button
    {
        const string k_CancelButtonText = "CANCEL";

        [CreateProperty, UxmlAttribute]
        public string SessionType
        {
            get => m_SessionType;
            set
            {
                if (m_SessionType == value)
                {
                    return;
                }

                m_SessionType = value;
                if (panel != null)
                {
                    UpdateBindings();
                }
            }
        }
        string m_SessionType;

        DataBinding m_DataBinding;

        public CancelMatchmakerSessionButton()
        {
            text = k_CancelButtonText;

            AddToClassList(BlocksTheme.Button);
            m_DataBinding = new DataBinding()
            {
                dataSourcePath = new PropertyPath(nameof(CancelMatchmakerSessionViewModel.CanCancel)),
                bindingMode = BindingMode.ToTarget
            };
            SetBinding(new BindingId(nameof(enabledSelf)), m_DataBinding);
            clicked += CancelSession;

            RegisterCallback<AttachToPanelEvent>(_ => UpdateBindings());
            RegisterCallback<DetachFromPanelEvent>(_ => CleanupBindings());
        }

        void CancelSession()
        {
            SessionCancellationUtils.CancelCancellationToken(m_SessionType);
        }

        void UpdateBindings()
        {
            CleanupBindings();
            m_DataBinding.dataSource = new CancelMatchmakerSessionViewModel(m_SessionType);
        }

        void CleanupBindings()
        {
            if (m_DataBinding.dataSource is IDisposable disposable)
            {
                disposable.Dispose();
            }
            m_DataBinding.dataSource = null;
        }
    }
}
