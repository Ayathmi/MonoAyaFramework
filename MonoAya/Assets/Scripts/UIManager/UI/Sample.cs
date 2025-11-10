using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MonoAya.UI
{
    public class Sample : MonoBehaviour ,IUIPanel, IController
    {
        public string PanelName { get; set; }
        public UILayer Layer { get; set; }
        public bool IsVisible { get; set; }
        public IFramework GetFramework() => App.Entry;

        public async void OnOpen()
        {
            await this.SendCommand(new OpenUICommand("AnotherSample", UILayer.Popup));
        }
    }
}