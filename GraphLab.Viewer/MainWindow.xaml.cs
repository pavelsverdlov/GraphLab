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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            lines = new List<Line>();
        }

        class GraphNode {
            public Ellipse Visual;
            public Vector2 Center;
        }

        List<GraphNode> vgraph;
        List<Line> lines;

        void ClearLines() {
            foreach (var l in lines) {
                canvas.Children.Remove(l);
            }
            lines.Clear();
        }
        private void NewGraphClick(object sender, RoutedEventArgs e) {
            canvas.Children.Clear();

            var width = (int)canvas.ActualWidth;
            var height = (int)canvas.ActualHeight;

            var iterarions = 20;
            var randomX = new Random();
            var randomY = new Random();

            var added = new HashSet<Vector2>();
            vgraph = new List<GraphNode>();

            var size = new Size(20, 20);
            var half = (float)size.Width * 0.5f;

            while (iterarions-- > 0) {
                var vertex = new Ellipse {
                    Fill = Brushes.Gray,
                    Width = size.Width,
                    Height = size.Height,
                    Tag = iterarions
                };

                var newpos = new Vector2(randomX.Next(0, width), randomY.Next(0, height));

                if (added.Add(newpos)) {
                    Canvas.SetLeft(vertex, newpos.X);
                    Canvas.SetTop(vertex, newpos.Y);

                    canvas.Children.Add(vertex);
                    vgraph.Add(new GraphNode {
                        Visual = vertex,
                        Center = new Vector2(newpos.X + half, newpos.Y + half)
                    });
                } else {
                    iterarions++;//try to refind new position
                    continue;
                }
            }

            
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            ClearLines();

            var graph = new GraphStructure(vgraph.Count);
            for (var i = 0; i < vgraph.Count; i++) {
                var from = vgraph[i];
                for (var j = 0; j < vgraph.Count; j++) {
                    var to = vgraph[j];
                    var val = (to.Center - from.Center).Length();
                    graph.AddEdge(i, j, val);
                    graph.AddEdge(j, i, val);
                }
            }
            var searcher = new Toolkit.Search.ACO(graph);
            var path = searcher.FindPath();

            var prev = vgraph[path[0].Index];
            for (var i = 1; i < vgraph.Count; i++) {
                var c = vgraph[path[i].Index];

                var line = new Line {
                    Stroke = Brushes.Green,
                    StrokeThickness = 2,
                };

                line.X1 = prev.Center.X;
                line.Y1 = prev.Center.Y;

                line.X2 = c.Center.X;
                line.Y2 = c.Center.Y;

                canvas.Children.Add(line);
                lines.Add(line);

                prev = c;
            }
        }
    }
}
