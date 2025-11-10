using System;
using UnityEngine;

namespace MonoAya
{
    public class UILauncher : MonoBehaviour, IController
    {
        public IFramework GetFramework() => App.Entry;

        private async void Start()
        {
            await this.SendCommand(new OpenUICommand("Sample", UILayer.Basic));
        }
    }
}