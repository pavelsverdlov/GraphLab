using MathNet.Numerics.LinearAlgebra;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GraphLab.Toolkit.Search {
    public enum ACOTypes {
        /// <summary>
        /// Ant System
        /// </summary>
        AS,
        /// <summary>
        /// Max-Min Ant System
        /// </summary>
        MMAS,
        /// <summary>
        /// Ant Colony System
        /// </summary>
        ACS,
    }
    public struct ACOSettings {
        public static ACOSettings Default() => new ACOSettings {
            Alfa = 1,
            Beta = 2f,
            ItreationCount = 100_000,
            NewPheromoneFactor = 1,
            DistanceInfluenceValue = 1f,
            EvaporationFactor = 0.7f,
            Type = ACOTypes.AS,
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
        public float NewPheromoneFactor { get; set; }
        /// <summary>
        /// Transition desirability coefficient depends on curve length,
        /// typically DistanceInfluence/Lxy, where L is the length
        /// </summary>
        public float DistanceInfluenceValue { get; set; }
        public float EvaporationFactor { get; set; }
        public ACOTypes Type { get; set; }
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

        readonly GraphStructure graph;
        readonly ACOSettings settings;
        
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
            List<GraphVertex> shortestPath= new List<GraphVertex>();
            var minPathLenght = float.MaxValue;
            //use some magnification because pheromones decrease to extra small value due to evaporation
            var magnificationFactor = settings.ItreationCount;

            try {
                var pheromones = new float[graph.V, graph.V];
                var endges = graph.GetEdges();

                //fill pheromones by coef of lenght 
                for (int i = 0; i < endges.Length; i++) {
                    var edge = endges[i];
                    var Lij = edge.Value;
                    pheromones[edge.From.Index, edge.To.Index] =    
                       //magnificationFactor; // Max-min Ant System
                     (settings.DistanceInfluenceValue / Lij) * magnificationFactor;
                }

                var pathVertices = new List<GraphVertex>();
                var pathEndges = new HashSet<Edge>();
                var processedVertices = new HashSet<GraphVertex>();
                var random = new Random();
                var pathLenght = 0f;

                var iterations = settings.ItreationCount;

                while (iterations --> 1) {
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
                                var rij = pheromones[edge.From.Index, edge.To.Index];
                                SumiN += (float)(Math.Pow(nij, settings.Beta) * Math.Pow(rij, settings.Alfa));
                            }
                        }

                        if (SumiN == 0) {
                            throw new Exception("SumiN = 0, no edge to pass"); 
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
                                //SumiN - summ each curves btw i-(N) N all avalible vertices connected to i
                                var Lij = edge.Value;
                                var nij = CaclulateSpecialScalar(Lij);
                                var rij = pheromones[edge.From.Index, edge.To.Index];
                                var Pij =
                                    ((MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa)) / SumiN)
                                    * 100f;// multiply by 100 to make range 0-100
                                checkSumm += Pij;
                                PiN.Add(Tuple.Create(Pij, edge));
                            }
                        }

                        if (MathF.Abs(checkSumm - 100f) > 0.01f) {
                            throw new Exception("Sum of all chances must be 100%");
                        }

                        //generate random value 0 - 100%
                        var hit = random.Next(0, 100);
                        var prev = 0f;
                        var usingEdge = new Edge { IndexFrom = next.Index };
                        next = default;
                        foreach (var chance in PiN) {
                            //search result which hits in range
                            if (prev <= hit && hit <= (prev + chance.Item1)) {
                                usingEdge.IndexTo = chance.Item2.To.Index;
                                pathEndges.Add(usingEdge);//keep passed edges

                                next = chance.Item2.To;
                                pathLenght += chance.Item2.Value;
                                break;
                            }
                            prev += chance.Item1;
                        }                       
                    }

                    if (pathLenght == 0 || pathVertices.Count < graph.V) {
                        throw new Exception("Invalid path, lenght is zero or not all vertices were passed.");
                    }
                   
                    if (minPathLenght > pathLenght) {
                        minPathLenght = pathLenght;
                        shortestPath = pathVertices;

                        //pheromone depends on the length of the chosen path:
                        //the shorter the path, the higher the amount of added pheromone
                        var dr = (settings.NewPheromoneFactor / pathLenght);// * magnificationFactor;

                        //pheromone evaporation coefficient
                        var p = settings.EvaporationFactor;

                        //loop over all edges
                        for (int i = 0; i < endges.Length; i++) {
                            var edge = endges[i];
                            var key = new Edge(edge.From.Index, edge.To.Index);

                            //new feromons rate
                            var drij = pathEndges.Contains(key) ?
                                dr  //if edge was used, apply new feromon value
                                : 0f;//only evaporation will effect on pheromone

                            var rij = pheromones[key.IndexFrom, key.IndexTo];

                            //update pheromones 
                            pheromones[key.IndexFrom, key.IndexTo] =
                                (1f - p) * rij // evaporation
                                + drij; // increase value of feromons based on found path

                        }
                    }
                }
            } catch (Exception ex) {
                ex.ToString();
            }
            return new GraphPath(shortestPath, minPathLenght);
        }

       
        /// <summary>
        /// just a help fuction to calculate scalar depending on length
        /// calls as 'a priori knowledge'
        /// </summary>
        /// <param name="Lij"></param>
        /// <returns></returns>
        float CaclulateSpecialScalar(float Lij) {
            //Lij - distance btw i-j

            return settings.DistanceInfluenceValue / Lij;
        }
    }
}
