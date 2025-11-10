using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MonoAya
{
    public class UIManager : ModelBase
    {
        private readonly Dictionary<string, IUIPanel> m_Panels = new();
        private readonly Stack<string> m_PanelStack = new();
        private readonly Dictionary<UILayer, Transform> m_LayerNode = new();

        private GameObject m_UIRoot;

        protected override void OnInitialization()
        {
            CreateUIRoot();
            CreateLayerParents();
        }
        protected override void OnShutdown()
        {
            CloseAllPanels();
            m_Panels.Clear();
            m_PanelStack.Clear();
            m_LayerNode.Clear();
        }

        public IUIPanel GetPanel(string identifier)
        {
            return m_Panels.GetValueOrDefault(identifier, null);
        }

        public bool IsPanelOpen(string identifier)
        {
            return m_Panels.ContainsKey(identifier) && m_Panels[identifier].IsVisible;
        }

        public void PushToStack(string identifier)
        {
            if (!m_PanelStack.Contains(identifier))
            {
                m_PanelStack.Push(identifier);
            }
        }
        
        public string PopFromStack()
        {
            return m_PanelStack.Count > 0 ? m_PanelStack.Pop() : null;
        }

        public void AddPanel(string panelName, IUIPanel panel)
        {
            m_Panels.TryAdd(panelName, panel);
        }

        public async UniTask<Transform> GetLayerNode(UILayer layer)
        {
            await UniTask.WaitUntil(() => m_LayerNode.TryGetValue(layer, out _));
            return m_LayerNode[layer];
        }
        
        public void RemovePanel(string identifier)
        {
            m_Panels.Remove(identifier);
        }

        private void CreateUIRoot()
        {
            var rootGo = new GameObject("UIRoot");
            m_UIRoot = rootGo;

            var canvas = rootGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = rootGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1;

            rootGo.AddComponent<GraphicRaycaster>();
        }

        private async void CreateLayerParents()
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                await UniTask.WaitUntil(() => m_UIRoot != null);
                var layerGo = new GameObject(layer.ToString());
                
                var rect = layerGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                var canvas = layerGo.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = (int)layer;

                var scaler = layerGo.AddComponent<CanvasScaler>();
                var raycast = layerGo.AddComponent<GraphicRaycaster>();

                m_LayerNode[layer] = layerGo.transform;
                layerGo.GetComponent<RectTransform>().SetParent(m_UIRoot.transform);
            }
        }

        private void CloseAllPanels()
        {
            foreach (var panel in m_Panels.Values)
            {
                if (panel != null)
                {
                    panel.OnClose();
                }
            }
        }
    }
    
    public interface IUIPanel
    {
        public string PanelName { get; set; }
        public UILayer Layer { get; set; }
        public bool IsVisible { get; set; }

        public void OnOpen() { }

        public void OnShow() { }
        
        public void OnHide() { }
        
        public virtual void OnClose() { }
    }

    public enum UILayer
    {
        Background = 0,
        Basic = 100,
        Popup = 200,
        Tutorial = 300,
        System = 400
    }

    public struct UIPanelOpenedEvent
    {
        public string Identifier;
        public UILayer Layer;
    }
    
    public struct UIPanelClosedEvent
    {
        public string Identifier;
    }
    
    public struct UIPanelShownEvent
    {
        public string Identifier;
    }
    
    public struct UIPanelHiddenEvent
    {
        public string Identifier;
    }
}