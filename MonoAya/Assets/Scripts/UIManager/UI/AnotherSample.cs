using UnityEngine;

namespace MonoAya.UI
{
    public class AnotherSample : MonoBehaviour ,IUIPanel, IController
    {
        public string PanelName { get; set; }
        public UILayer Layer { get; set; }
        public bool IsVisible { get; set; }
        public IFramework GetFramework() => App.Entry;
        
        public void OnOpen()
        {
            Debug.Log("Another Logging!");
        }
    }
}