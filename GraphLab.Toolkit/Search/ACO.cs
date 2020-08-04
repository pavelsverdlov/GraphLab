using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GraphLab.Toolkit.Search {

    public struct ACOSettings {
        public static ACOSettings Default() => new ACOSettings {
            Alfa = 1,
            Beta = 2f,
            ItreationCount = 1000,
            NewFactor = 1,
            DistanceInfluenceValue = 1f,
            EvaporationFactor = 0.1f,
        };
        // >=0
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Alfa = 0 - make it as 'Greedy algorithm'
        /// </remarks>
        public float Alfa { get; set; }
        //>=1
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Alfa = 0 - igonre curve lenght, focus only on pheromones
        /// </remarks>
        public float Beta { get; set; }
        public int ItreationCount { get; set; }
        /// <summary>
        /// constant uses for calculation new pheromones factor (Q/L)
        /// L - path lenght
        /// </summary>
        public float NewFactor { get; set; }
        /// <summary>
        /// Transition desirability coefficient depends on curve length,
        /// typically DistanceInfluence/Lxy, where L is the length
        /// </summary>
        public float DistanceInfluenceValue { get; set; }
        public float EvaporationFactor { get; set; }

    }

    /// <summary>
    /// Ant colony optimization algorithms
    /// 
    /// </summary>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms
    /// https://www.ics.uci.edu/~welling/teaching/271fall09/antcolonyopt.pdf
    /// https://www.researchgate.net/publication/308953674_Ant_Colony_Optimization
    /// </remarks>
    public class ACO : GraphSearcher {
        struct Edge : IEquatable<Edge> {
            public int IndexFrom;
            public int IndexTo;

            public Edge(int indexFrom, int indexTo) {
                IndexFrom = indexFrom;
                IndexTo = indexTo;
            }

            public override bool Equals(object? obj) {
                return obj is Edge edge && Equals(edge);
            }

            public bool Equals(Edge other) {
                return IndexFrom == other.IndexFrom &&
                       IndexTo == other.IndexTo;
            }

            public override int GetHashCode() {
                return HashCode.Combine(IndexFrom, IndexTo);
            }
            public override string ToString() => $"[{IndexFrom}-{IndexTo}]";
        }

        readonly Dictionary<Edge, float> pheromone = new Dictionary<Edge, float>();
        readonly GraphStructure graph;
        readonly ACOSettings settings;
        Matrix<float> pheromones;

        public ACO(GraphStructure graph, ACOSettings settings) {
            this.graph = graph;
            this.settings = settings;
        }
        public override GraphPath FindPath(GraphVertex start) {
            return FindPath(g => start);
        }
        public override GraphPath FindPath() {
            var randomStart = new Random();
            return FindPath(g => g.GetVertex(randomStart.Next(0, g.V)));
        }
        GraphPath FindPath(Func<GraphStructure, GraphVertex> getStartVertex) {
            var paths = new SortedList<float, List<GraphVertex>>();
            var minPathLenght = float.MaxValue;

            try {
                pheromones = Matrix<float>.Build.Dense(graph.V, graph.V, 0f);
                var endges = graph.GetEdges();
                var pathVertices = new List<GraphVertex>();
                var pathEndges = new HashSet<Edge>();
                var processedVertices = new HashSet<GraphVertex>();
                var random = new Random();
                var pathLenght = 0f;

                var iterations = settings.ItreationCount;

                while (iterations-- > 1) {
                    pathLenght = 0f;
                    pathVertices = new List<GraphVertex>();
                    processedVertices.Clear();
                    pathEndges.Clear();

                    var next = getStartVertex(graph);

                    while (next.IsValid) {
                        processedVertices.Add(next);
                        pathVertices.Add(next);

                        if (pathVertices.Count == graph.V) {
                            break;//all nodes were passed
                        }

                        var i = next;

                        float SumiN = 0f;
                        var neighbors = graph.GetNeighbors(i);
                        foreach (var edge in neighbors) {
                            if (!processedVertices.Contains(edge.To)) {
                                var Lij = edge.Value;
                                var nij = CaclulateSpecialScalar(Lij);
                                var rij = GetEdgePheromoneOrDefault(edge);
                                SumiN += (float)(Math.Pow(nij, settings.Beta) * Math.Pow(rij, settings.Alfa));
                            }
                        }

                        //Ant Colony System optimization approach
                        //choice of next step based on a random generation

                        //first prepare percentage chance of each possible passes 
                        var PiN = new List<Tuple<float, GraphEdge>>();
                        var checkSumm = 0f;
                        foreach (var edge in neighbors) {
                            if (!processedVertices.Contains(edge.To)) {
                                //calculate transition probability
                                //Lij - distance btw i-j
                                //rij - ant feromons btw i-j 
                                //Sumij - summ each curves btw i-(N) N each vertices connected to i
                                var Lij = edge.Value;
                                var nij = CaclulateSpecialScalar(Lij);
                                var rij = GetEdgePheromoneOrDefault(edge);
                                var Pij = 
                                    ((MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa)) / SumiN)
                                    * 100f;// multiply by 100 to make range 0-100
                                checkSumm += Pij;
                                PiN.Add(Tuple.Create(Pij, edge));
                            }
                        }

                        if(Math.Abs(checkSumm - 100f) > 0.001f) {
                            throw new Exception("Sum of all chances must be 100%");
                        }

                        //generate random value 0 - 100%
                        var chance = random.Next(0, 100);
                        var prev = 0f;
                        var usingEdge = new Edge { IndexFrom = next.Index};
                        next = default;
                        foreach (var val in PiN) {
                            //search result which hits in range
                            if (prev <= chance && chance <= (prev + val.Item1)) {
                                usingEdge.IndexTo = val.Item2.To.Index;
                                next = val.Item2.To;
                                pathLenght += val.Item2.Value;
                                break;
                            }
                            prev += val.Item1;
                        }

                        pathEndges.Add(usingEdge);                        
                    }
                    if(pathLenght == 0) {
                        throw new Exception("Path length can be 0.");
                    }

                    if (paths.ContainsKey(pathLenght) || pathVertices.Count < graph.V) {
                        iterations++;//increase because current iteration is waste
                        continue;
                    } else {
                        paths.Add(pathLenght, pathVertices);
                    }

                    //update pheromones only if path is the shortest 
                    if (minPathLenght > pathLenght) {
                        minPathLenght = pathLenght;

                        
                    }
                    //pheromone depends on the length of the chosen path:
                    //the shorter the path, the higher the amount of added pheromone
                    var dr = settings.NewFactor / pathLenght;

                    //pheromone evaporation coefficient
                    var p = settings.EvaporationFactor;

                    //loop over all edges
                    for (int i = 0; i < endges.Length; i++) {
                        var edge = endges[i];
                        var key = new Edge(edge.From.Index, edge.To.Index);

                        //new feromons rate
                        var drij = 0f;//only evaporation will effect on pheromone
                        if (pathEndges.Contains(key)) {
                            drij = dr;
                        }

                        if (pheromone.ContainsKey(key)) {
                            var rij = pheromone[key];
                            //Pheromone update
                            pheromone[key] =
                                (1f - p) * rij // evaporation
                                + drij; // increase value of feromons based on found path
                        } else if (pathEndges.Contains(key)) {
                            //first time of passing this edge, no evaporation
                            pheromone[key] = drij;
                        }
                    }
                }
            } catch (Exception ex) {
                ex.ToString();
            }
            return new GraphPath(paths[minPathLenght], minPathLenght);
        }

        float GetEdgePheromoneOrDefault(GraphEdge edge) {
            var key = new Edge(edge.From.Index, edge.To.Index);
            if (!pheromone.ContainsKey(key)) {
                ////if ant didn't traverse through this vertex, so use default function to calculate feromon
                return CaclulateSpecialScalar(edge.Value);
                //return 0.0001f;//TODO
            }
            return pheromone[key];
        }
        /// <summary>
        /// just a help fuction to calculate scalar depending on length
        /// calls as 'a priori knowledge'
        /// </summary>
        /// <param name="Lij"></param>
        /// <returns></returns>
        float CaclulateSpecialScalar(float Lij) {
            //Lij - distance btw i-j

            return (settings.DistanceInfluenceValue / Lij);
        }
    }
}
