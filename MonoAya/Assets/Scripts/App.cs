namespace MonoAya
{
    public class App : Framework<App>
    {
        protected override async void Initialize()
        {
           RegisterModel<UIManager>(new UIManager());
        }
    }
}