using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace MonoAya
{
    public class OpenUICommand : AsyncCommandBase
    {
        private readonly string m_Identifier;
        private readonly UILayer m_Layer;
        private readonly bool m_AddToStack;
        
        public OpenUICommand(string identifier, UILayer layer, bool addToStack = true)
        {
            m_Identifier = identifier;
            m_Layer = layer;
            m_AddToStack = addToStack;
        }

        // Notice: Rewrite or Override this method to adapt your assets loading workflow
        protected virtual async UniTask<GameObject> LoadPanelPrefabAsync(string identifier)
        {
            var prefab = await Addressables.LoadAssetAsync<GameObject>(identifier);
            return prefab;
        }
        
        protected override async UniTask OnExecuteAsync()
        {
            var uiManager = this.GetModel<UIManager>();

            var existingPanel = uiManager.GetPanel(m_Identifier);
            if (existingPanel is { IsVisible: true }) return;

            var prefab = await LoadPanelPrefabAsync(m_Identifier);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load panel prefab: {m_Identifier}");
                return;
            }

            var layer = uiManager.GetLayerNode(m_Layer);
            var panelObj = GameObject.Instantiate(prefab, await layer);
            var panel = panelObj.GetComponent<IUIPanel>();

            if (panel == null)
            {
                Debug.LogError($"Panel prefab {m_Identifier} is missing a UIPanel component.");
                GameObject.Destroy(panelObj);
                return;
            }

            panel.PanelName = m_Identifier;
            panel.Layer = m_Layer;
            
            uiManager.AddPanel(m_Identifier, panel);

            if (m_AddToStack)
            {
                uiManager.PushToStack(m_Identifier);
            }
            
            panel.OnOpen();
            
            this.SendEvent(new UIPanelOpenedEvent
            {
                Identifier = m_Identifier,
                Layer = m_Layer
            });
        }
    }

    public class CloseUICommand : AsyncCommandBase
    {
        private readonly string m_Identifier;
        private readonly bool m_Destroy;
        
        public CloseUICommand(string identifier, bool destroy = true)
        {
            m_Identifier = identifier;
            m_Destroy = destroy;
        }
        
        protected override async UniTask OnExecuteAsync()
        { 
            var uiManager = this.GetModel<UIManager>();
            var panel = uiManager.GetPanel(m_Identifier);
            
            if (panel == null) return;

            if (m_Destroy)
            {
                panel.OnClose();
                uiManager.RemovePanel(m_Identifier);
            }
            else
            {
                panel.OnHide();
            }
            
            this.SendEvent(new UIPanelClosedEvent
            {
                Identifier = m_Identifier
            });

            await UniTask.Yield();
        }
    }

    public class ShowUIPanelCommand : AsyncCommandBase
    {
        private readonly string m_Identifier;
        
        public ShowUIPanelCommand(string identifier)
        {
            m_Identifier = identifier;
        }
        
        protected override async UniTask OnExecuteAsync()
        {
            var uiManager = this.GetModel<UIManager>();
            var panel = uiManager.GetPanel(m_Identifier);
            
            if (panel == null) return;

            panel.OnShow();
            
            this.SendEvent(new UIPanelShownEvent
            {
                Identifier = m_Identifier
            });

            await UniTask.Yield();
        }
    }
    
    public class HideUIPanelCommand : AsyncCommandBase
    {
        private readonly string m_Identifier;
        
        public HideUIPanelCommand(string identifier)
        {
            m_Identifier = identifier;
        }
        
        protected override async UniTask OnExecuteAsync()
        {
            var uiManager = this.GetModel<UIManager>();
            var panel = uiManager.GetPanel(m_Identifier);
            
            if (panel == null) return;

            panel.OnHide();
        }
    }

    public class GoBackUICommand : AsyncCommandBase
    {
        protected override async UniTask OnExecuteAsync()
        {
            var uiManager = this.GetModel<UIManager>();
            var currentIdentifier = uiManager.PopFromStack();

            if (!string.IsNullOrEmpty(currentIdentifier))
            {
                await this.SendCommand(new CloseUICommand(currentIdentifier));
            }

            var previousIdentifier = uiManager.PopFromStack();
            if (!string.IsNullOrEmpty(previousIdentifier))
            {
                await this.SendCommand(new ShowUIPanelCommand(previousIdentifier));
                uiManager.PushToStack(previousIdentifier);
            }
        }
    }
}