using GraphLab.Toolkit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFLab;
using WPFLab.MVVM;
using WPFLab.PropertiesEditer;

namespace GraphLab.Viewer {
    class GraphPropertiesProxy : FluentValidation.AbstractValidator<GraphPropertiesProxy>, IViewValidator {
        [Display]
        [Editable(true)]
        public float Alfa { get; set; }
        [Display]
        [Editable(true)]
        public float Beta { get; set; }
        [Display]
        [Editable(true)]
        public int ItreationCount { get; set; }
        [Display]
        [Editable(true)]
        public float NewPheromoneFactor { get; set; }
        [Display]
        [Editable(true)]
        public float DistanceInfluenceValue { get; set; }
        [Display]
        [Editable(true)]
        public float EvaporationFactor { get; set; }

        public FluentValidation.Results.ValidationResult Validate() => Validate(this);
    }
    class GraphPathItem {
        public readonly GraphPath Path;
        public readonly GraphEccentricity Eccentricity;

        public float Lenght => Path.Lenght;
        public string Date { get; }
        public int Iterations { get; }
        public GraphPathItem(GraphPath path, GraphEccentricity ecc, int iterations, TimeSpan time) {
            this.Path = path;
            Date = time.ToString();
            this.Eccentricity = ecc;
            Iterations = iterations;
        }
    }
    class GraphCreationData : FluentValidation.AbstractValidator<GraphCreationData>, IViewValidator {
        [Display]
        [Editable(true)]
        public int VertexCount { get; set; }

        public FluentValidation.Results.ValidationResult Validate() => Validate(this);
    }
    class MainViewModel : BaseNotify, ISurfaceSupport {
        class GraphNode {
            public Ellipse Visual;
            public Vector2 Center;
        }

        public bool UseRandom {
            get => useRandom;
            set {
                Update(ref useRandom, value);
                useEccentricityBorder = useEccentricityCenter = !value;
                SetPropertyChanged(nameof(UseEccentricityCenter));
                SetPropertyChanged(nameof(UseEccentricityBorder));
            }
        }
        public bool UseEccentricityCenter {
            get => useEccentricityCenter;
            set {
                Update(ref useEccentricityCenter, value);
                useEccentricityBorder = useRandom = !value;
                SetPropertyChanged(nameof(UseEccentricityBorder));
                SetPropertyChanged(nameof(UseRandom));
            }
        }
        public bool UseEccentricityBorder {
            get => useEccentricityBorder;
            set {
                Update(ref useEccentricityBorder, value);
                useEccentricityCenter = useRandom = !value;
                SetPropertyChanged(nameof(UseEccentricityCenter));
                SetPropertyChanged(nameof(UseRandom));
            }
        }

        public Visibility ProgressBarVisibility {
            get => progressBarVisibility;
            set => Update(ref progressBarVisibility, value); 
        }

        public ICommand NewGraphCommand { get; }
        public ICommand FindPathCommand { get; }
        public GroupViewProperty<GraphCreationData> GraphCreationProperties { get; }
        public GroupViewProperty<GraphPropertiesProxy> GraphProperties {
            get => graphProperties;
            set => Update(ref graphProperties, value, nameof(GraphProperties));
        }
        public ICollectionView FoundPaths { get; }

        Canvas canvas;
        GroupViewProperty<GraphPropertiesProxy> graphProperties;

        bool useRandom;
        bool useEccentricityCenter;
        bool useEccentricityBorder;
        private Visibility progressBarVisibility;
        readonly List<GraphNode> vgraph;
        readonly List<Line> lines;
        readonly GraphLabMapperService mapper;
        readonly ObservableCollection<GraphPathItem> foundPaths;
        public MainViewModel(GraphLabMapperService mapper) {
            vgraph = new List<GraphNode>();
            lines = new List<Line>();
            NewGraphCommand = new WpfActionCommand(OnNewGraph);
            FindPathCommand = new WpfActionCommand(OnFindPath);
            this.mapper = mapper;
            graphProperties = new GroupViewProperty<GraphPropertiesProxy>(
                mapper.Map<Toolkit.Search.ACOSettings, GraphPropertiesProxy>(Toolkit.Search.ACOSettings.Default()),
                "title");
            graphProperties.Analyze();

            foundPaths = new ObservableCollection<GraphPathItem>();
            FoundPaths = CollectionViewSource.GetDefaultView(foundPaths);
            FoundPaths.CurrentChanged += FoundPaths_CurrentChanged;

            GraphCreationProperties = new GroupViewProperty<GraphCreationData>(new GraphCreationData {
                VertexCount = 20,

            }, "title");
            GraphCreationProperties.Analyze();
            useRandom = true;
            ProgressBarVisibility = Visibility.Collapsed;
        }

        void FoundPaths_CurrentChanged(object? sender, EventArgs e) {
            if (FoundPaths.CurrentItem is GraphPathItem item) {
                ClearLines();
                var gpath = item;
                vgraph[item.Eccentricity.Center.Index].Visual.Fill = Brushes.Blue;
                vgraph[item.Eccentricity.Border.Index].Visual.Fill = Brushes.Red;

                var path = gpath.Path.Vertices;

                var prev = vgraph[path[0].Index];
                for (var i = 1; i < path.Count; i++) {
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

        public void SetSurface(Canvas canvas) {
            this.canvas = canvas;
        }

        void ClearLines() {
            foreach (var l in lines) {
                canvas.Children.Remove(l);
            }
            lines.Clear();
            foreach (var l in vgraph) {
                l.Visual.Fill = Brushes.Gray;
            }
        }

        async void OnFindPath() {
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

            var ecc = graph.CalculateEccentricity();
            var settings = mapper.Map<GraphPropertiesProxy, Toolkit.Search.ACOSettings>(graphProperties.Value);
            var searcher = new Toolkit.Search.ACO(graph, settings);

            GraphPath gpath = null;
            ProgressBarVisibility = Visibility.Visible;
            var sw = new Stopwatch();
            sw.Start();
            await Task.Run(() => {
                if (UseRandom) {
                    gpath = searcher.FindPath();
                } else {
                    gpath = searcher.FindPath(UseEccentricityCenter ? ecc.Center : ecc.Border);
                }
            });
            sw.Stop();
            ProgressBarVisibility = Visibility.Collapsed;
            foundPaths.Insert(0, new GraphPathItem(gpath, ecc, settings.ItreationCount, sw.Elapsed));
            FoundPaths.MoveCurrentToFirst();
        }

        void OnNewGraph() {
            foundPaths.Clear();
            canvas.Children.Clear();

            //OnCreateK6Graph();
            //return;

            var width = (int)canvas.ActualWidth;
            var height = (int)canvas.ActualHeight;

            var iterarions = GraphCreationProperties.Value.VertexCount;
            var randomX = new Random((int)DateTime.Now.Ticks);
            var randomY = new Random((int)DateTime.Now.Ticks);

            var added = new HashSet<Vector2>();
            vgraph.Clear();

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
        void OnCreateK6Graph() {
            foundPaths.Clear();
            canvas.Children.Clear();

            var width = (int)canvas.ActualWidth;
            var height = (int)canvas.ActualHeight;
            var halfw = width * 0.5f;
            var halfh = height * 0.5f;

            var iterarions = 20;
            var randomX = new Random();
            var randomY = new Random();

            var added = new HashSet<Vector2>();
            vgraph.Clear();

            var size = new Size(20, 20);
            var halfSize = (float)size.Width * 0.5f;


            //top
            var vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 0
            };
            var newpos = new Vector2(halfw, 0);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //bottom
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(halfw, height - (float)size.Height);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //left
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(halfw * 0.5f, halfh * 0.5f);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2((float)size.Width, halfh);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(halfw * 0.5f, halfh * 1.5f);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //right
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(halfw * 1.5f, halfh * 0.5f);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(width - (float)size.Width, halfh);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });
            //
            vertex = new Ellipse {
                Fill = Brushes.Gray,
                Width = size.Width,
                Height = size.Height,
                Tag = 1
            };
            newpos = new Vector2(halfw * 1.5f, halfh * 1.5f);
            added.Add(newpos);
            Canvas.SetLeft(vertex, newpos.X);
            Canvas.SetTop(vertex, newpos.Y);

            canvas.Children.Add(vertex);
            vgraph.Add(new GraphNode {
                Visual = vertex,
                Center = new Vector2(newpos.X + halfSize, newpos.Y + halfSize)
            });

        }

    }
}
