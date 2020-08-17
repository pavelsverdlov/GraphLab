using FluentValidation;

using GraphLab.Toolkit;
using GraphLab.Toolkit.Search;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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
using WPFLab.PropertyGrid;

namespace GraphLab.Viewer {
    class GraphPropertiesProxy : ViewModelProxy<GraphPropertiesProxy>, IViewModelProxy {
        private ACOTypes type;

        [Visible]
        [Display]
        [ReadOnly(false)]
        public float Alfa { get; set; }
        [Visible]
        [Display]
        public float Beta { get; set; }
        [Visible]
        [Display]
        public int ItreationCount { get; set; }
        [Visible]
        [Display(Name = "Distance influence on pheromone")]
        public float DistanceInfluenceOnPheromoneFactor { get; set; }
        [Visible]
        [Display(Name = "Distance influence")]
        public float DistanceInfluenceFactor { get; set; }
        [Visible]
        [Display(Name = "Evaporation")]
        public float EvaporationFactor { get; set; }
        [Visible]
        [Display]
        public ACOTypes Type { 
            get => type;
            set {
                type = value;
            }
        }

        public GraphPropertiesProxy() {
            RuleFor(x => x.EvaporationFactor)
                .InclusiveBetween(0f, 1f);
            RuleFor(x => x.DistanceInfluenceFactor)
                .GreaterThan(0f);
            RuleFor(x => x.DistanceInfluenceOnPheromoneFactor)
                .GreaterThan(0f);
            RuleFor(x => x.Alfa)
               .GreaterThan(0f);
            RuleFor(x => x.Beta)
               .GreaterThan(0f);
        }

        protected override FluentValidation.Results.ValidationResult OnValidate() => Validate(this);
    }
    class GraphPathItem {
        public readonly GraphPath Path;
        public readonly GraphEccentricity Eccentricity;

        public float Lenght => Path.Lenght;
        public string Date { get; }
        public int Iterations { get; }
        public ACOTypes Type { get; }

        public GraphPathItem(GraphPath path, GraphEccentricity ecc, int iterations, ACOTypes type, TimeSpan time) {
            this.Path = path;
            Date = time.ToString();
            this.Eccentricity = ecc;
            Iterations = iterations;
            Type = type;
        }
    }
    class GraphCreationData : ViewModelProxy<GraphCreationData> {
        [Visible]
        [Display(Name ="Vertex count")]
        [Editable(true)]
        public int VertexCount { get; set; }

        public GraphCreationData() {
            RuleFor(x => x.VertexCount)
              .GreaterThan(1);
        }

        protected override FluentValidation.Results.ValidationResult OnValidate() => Validate(this);
    }
    class MainViewModel : BaseNotify, ISurfaceSupport {
        class GraphNode {
            public Ellipse Visual;
            public Vector2 Center;
        }

        public bool UseAllVertices {
            get => useAllVertices;
            set {
                Update(ref useAllVertices, value);
                useRandomVertices = useEccentricityBorder = useEccentricityCenter = !value;
                SetPropertyChanged(nameof(UseRandomVertices));
                SetPropertyChanged(nameof(UseEccentricityCenter));
                SetPropertyChanged(nameof(UseEccentricityBorder));
            }
        }
        public bool UseRandomVertices {
            get => useRandomVertices;
            set {
                Update(ref useRandomVertices, value);
                useAllVertices = useEccentricityBorder = useEccentricityCenter = !value;
                SetPropertyChanged(nameof(UseAllVertices));
                SetPropertyChanged(nameof(UseEccentricityCenter));
                SetPropertyChanged(nameof(UseEccentricityBorder));
            }
        }
        public bool UseEccentricityCenter {
            get => useEccentricityCenter;
            set {
                Update(ref useEccentricityCenter, value);
                useRandomVertices = useEccentricityBorder = useAllVertices = !value;
                SetPropertyChanged(nameof(UseRandomVertices));
                SetPropertyChanged(nameof(UseEccentricityBorder));
                SetPropertyChanged(nameof(UseAllVertices));
            }
        }
        public bool UseEccentricityBorder {
            get => useEccentricityBorder;
            set {
                Update(ref useEccentricityBorder, value);
                useRandomVertices = useEccentricityCenter = useAllVertices = !value;
                SetPropertyChanged(nameof(UseRandomVertices));
                SetPropertyChanged(nameof(UseEccentricityCenter));
                SetPropertyChanged(nameof(UseAllVertices));
            }
        }

        public Visibility ProgressBarVisibility {
            get => progressBarVisibility;
            set => Update(ref progressBarVisibility, value);
        }

        public ICommand NewGraphCommand { get; }
        public ICommand FindPathCommand { get; }
        public GroupViewModelProperties<GraphCreationData> GraphCreationProperties { get; }
        public GroupViewModelProperties<GraphPropertiesProxy> GraphProperties {
            get => graphProperties;
            set => Update(ref graphProperties, value, nameof(GraphProperties));
        }
        public ICollectionView FoundPaths { get; }
        public bool IsAllParamsValid { 
            get => isAllParamsValid;
            set => Update(ref isAllParamsValid, value);
        }
        public bool AllowCreateGraph {
            get => allowCreateGraph;
            set => Update(ref allowCreateGraph, value);
        }
        

        Canvas? canvas;
        GroupViewModelProperties<GraphPropertiesProxy> graphProperties;

        bool useAllVertices;
        bool useRandomVertices;
        bool useEccentricityCenter;
        bool useEccentricityBorder;
        Visibility progressBarVisibility;
        bool isAllParamsValid;
        bool allowCreateGraph;
        readonly List<GraphNode> vgraph;
        readonly List<Shape> lines;
        readonly GraphLabMapperService mapper;
        readonly ObservableCollection<GraphPathItem> foundPaths;
        public MainViewModel(GraphLabMapperService mapper) {
            vgraph = new List<GraphNode>();
            lines = new List<Shape>();
            NewGraphCommand = new WpfActionCommand(OnNewGraph);
            FindPathCommand = new WpfActionCommand(OnFindPath);
            this.mapper = mapper;
            graphProperties = new GroupViewModelProperties<GraphPropertiesProxy>(
                mapper.Map<Toolkit.Search.ACOSettings, GraphPropertiesProxy>(Toolkit.Search.ACOSettings.Default()),
                "title");
            graphProperties.Analyze();

            foundPaths = new ObservableCollection<GraphPathItem>();
            FoundPaths = CollectionViewSource.GetDefaultView(foundPaths);
            FoundPaths.CurrentChanged += FoundPaths_CurrentChanged;

            GraphCreationProperties = new GroupViewModelProperties<GraphCreationData>(new GraphCreationData {
                VertexCount = 20,
            }, "title");
            GraphCreationProperties.Analyze();
            useRandomVertices = true;
            ProgressBarVisibility = Visibility.Collapsed;

            graphProperties.Value.ValidationStatusChanged += GraphPropertiesValidationStatusChanged;
            GraphCreationProperties.Value.ValidationStatusChanged += CreationPropertiesValidationStatusChanged;

            AllowCreateGraph = true;
        }

        void GraphPropertiesValidationStatusChanged(bool isValid) {
            IsAllParamsValid = isValid && vgraph.Any();
        }
        void CreationPropertiesValidationStatusChanged(bool isValid) {
            AllowCreateGraph = isValid;
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
                    var next = vgraph[path[i].Index];

                    var line = new Line {
                        Stroke = Brushes.Green,
                        StrokeThickness = 2,
                    };
                    line.X1 = prev.Center.X;
                    line.Y1 = prev.Center.Y;

                    line.X2 = next.Center.X;
                    line.Y2 = next.Center.Y;

                    //arrow
                    var bow = new Point(next.Center.X, next.Center.Y);
                    var vector = new Point(prev.Center.X, prev.Center.Y) - bow;
                    vector *= .3f;
                    if (vector.Length > 30f) {
                        vector.Normalize();
                        vector *= 30f;
                    }
                    var rotate = new Matrix();
                    rotate.RotateAt(20, bow.X, bow.Y);
                    var points = new PointCollection();
                    points.Add(bow);
                    points.Add(bow + rotate.Transform(vector));
                    rotate.Invert();
                    points.Add(bow + rotate.Transform(vector));
                    var arrow = new Polygon {
                        Fill = Brushes.Green,
                        Points = points,
                    };

                    canvas.Children.Add(line);
                    lines.Add(line);

                    canvas.Children.Add(arrow);
                    lines.Add(arrow);

                    prev = next;
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
            var searcher = GraphSearcher.CreateACO(graph, settings);

            GraphPath gpath = null;
            ProgressBarVisibility = Visibility.Visible;
            var sw = new Stopwatch();
            sw.Start();
            await Task.Run(() => {
                if (UseAllVertices) {
                    gpath = searcher.FindPath(StartVertexOptions.AllVertices);
                } else if (UseRandomVertices) {
                    gpath = searcher.FindPath(StartVertexOptions.RandomVertex);
                } else {
                    gpath = searcher.FindPath(UseEccentricityCenter ? ecc.Center : ecc.Border);
                }
            });
            sw.Stop();
            ProgressBarVisibility = Visibility.Collapsed;
            foundPaths.Insert(0, new GraphPathItem(gpath, ecc, settings.ItreationCount, settings.Type, sw.Elapsed));
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
            graphProperties.Validate();
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
