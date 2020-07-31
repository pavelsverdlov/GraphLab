using GraphLab.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GraphLab.Viewer {
    public interface ISurfaceSupport {
        void SetSurface(Canvas canvas);
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            DataContextChanged += MainWindow_DataContextChanged;
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if(e.NewValue is ISurfaceSupport support) {
                support.SetSurface(canvas);
            }
        }

    }
}
