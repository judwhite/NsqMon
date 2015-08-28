using System.Windows;
using System.Windows.Threading;
using NsqMon.Common;
using NsqMon.Common.ApplicationServices;

namespace NsqMon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //Resources = (ResourceDictionary)LoadComponent(new Uri("/Themes/Default/Theme.xaml", UriKind.RelativeOrAbsolute));

            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            IoC.Resolve<IDialogService>().ShowError(e.Exception);
            e.Handled = true;
        }
    }
}
