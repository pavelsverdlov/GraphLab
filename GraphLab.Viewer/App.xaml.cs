using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WPFLab;

namespace GraphLab.Viewer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : LabApplication {
        protected override void AppExit(ExitEventArgs e, IDependencyResolverService resolver) {
          
        }

        protected override void AppStartup(StartupEventArgs e, IDependencyResolverService resolver) {
            resolver.ResolveView<MainWindow, MainViewModel>().Show();
        }

        protected override void ConfigureServices(IDependencyRegisterService registrator) {
            registrator
                .RegisterView<MainWindow>()
                
                .Register<GraphLabMapperService>()
                .Register<MainViewModel>()

                .RegisterMvvm();
        }
    }
}
