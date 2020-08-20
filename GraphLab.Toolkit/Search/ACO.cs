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
        /// Ant Colony System
        /// </summary>
        ACS
    }
    public struct ACOSettings {
        public static ACOSettings Default() => new ACOSettings {
            Alfa = 1,
            Beta = 2f,
            IterationCount = 10_000,
            DistanceInfluenceOnPheromoneFactor = 1,
            DistanceInfluenceFactor = 1f,
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
        /// Beta = 0 - igonre curve lenght, focus only on pheromones
        /// </remarks>
        public float Beta { get; set; }
        public int IterationCount { get; set; }
        /// <summary>
        /// constant uses for calculation new pheromones factor (Q/L)
        /// L - path lenght
        /// </summary>
        public float DistanceInfluenceOnPheromoneFactor { get; set; }
        /// <summary>
        /// Transition desirability coefficient depends on curve length,
        /// typically DistanceInfluence/Lxy, where L is the length
        /// </summary>
        public float DistanceInfluenceFactor { get; set; }
        public float EvaporationFactor { get; set; }
        public ACOTypes Type { get; set; }
    }

    /// <summary>
    /// Ant colony optimization algorithms    /// 
    /// </summary>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms
    /// https://www.ics.uci.edu/~welling/teaching/271fall09/antcolonyopt.pdf
    /// https://www.researchgate.net/publication/308953674_Ant_Colony_Optimization
    /// http://www.scholarpedia.org/article/Ant_colony_optimization
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
        readonly float[,] pheromones;
        readonly GraphEdge[] edges;
        int nextVertexIndex;
        readonly float magnificationFactor;
        readonly Random startVertexRandom;

        internal ACO(GraphStructure graph, ACOSettings settings) {
            this.graph = graph;
            this.settings = settings;
            pheromones = new float[graph.Count, graph.Count];
            edges = graph.GetEdges();

            //in case big EvaporationFactor, use some magnification because pheromones decrease to extra small value due to evaporation
            magnificationFactor = settings.IterationCount;
            startVertexRandom = new Random((int)DateTime.Now.Ticks);
        }

        public override GraphPath FindPath(GraphVertex start) {
            return FindPath(() => start);
        }

        public override GraphPath FindPath(StartVertexOptions option) {
            return FindPath(
                option == StartVertexOptions.AllVertices ?
                    GetNextVertex()
                    : GetRandomVertex());
        }

        GraphVertex GetNextVertex() {
            if (nextVertexIndex == 0) {
                nextVertexIndex = graph.Count - 1;
            }
            var v = graph.GetVertex(nextVertexIndex);
            nextVertexIndex--;
            return v;
        }
        GraphVertex GetRandomVertex() {
            return graph.GetVertex(startVertexRandom.Next(0, graph.Count));
        }

        void FillInitialPheromones() {
            //by coef of lenght 
            for (int i = 0; i < edges.Length; i++) {
                var edge = edges[i];
                var dij = edge.Value;
                pheromones[edge.From.Index, edge.To.Index] =
                 (settings.DistanceInfluenceFactor / dij) * magnificationFactor;
            }
        }

        GraphPath FindPath(Func<GraphVertex> getStartVertex) {
            List<GraphVertex> shortestPath = new List<GraphVertex>();
            var minPathLength = float.MaxValue;

            FillInitialPheromones();

            var pathEdges = new HashSet<Edge>();
            var processedVertices = new HashSet<GraphVertex>();
            var random = new Random();

            var iterations = settings.IterationCount;
            //pheromone evaporation coefficient
            var p = settings.EvaporationFactor;

            while (iterations-- > 1) {
                var pathLength = 0f;
                var pathVertices = new List<GraphVertex>();
                processedVertices.Clear();
                pathEdges.Clear();

                var next = getStartVertex();

                while (next.IsValid) {
                    processedVertices.Add(next);
                    pathVertices.Add(next);

                    if (pathVertices.Count == graph.Count) {
                        break;//all nodes were passed
                    }

                    //SumiN - summ each curves btw i-(N) N all avalible vertices connected to i
                    float SumiN = 0f;
                    var neighbors = graph.GetNeighbors(next);
                    foreach (var edgeij in neighbors) {
                        if (!processedVertices.Contains(edgeij.To)) {
                            var Lij = edgeij.Value;
                            var nij = CaclulateMoveAttractiveness(Lij) * magnificationFactor;
                            var rij = pheromones[edgeij.From.Index, edgeij.To.Index];
                            SumiN += MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa);
                        }
                    }

                    if (SumiN == 0) {
                        throw new Exception("SumiN is ZERO, no edge to pass");
                    }

                    GraphEdge bestEdgeToMove = default;

                    switch (settings.Type) {//analyze the next step 
                        case ACOTypes.AS:

                            #region  Ant System
                            var bestMove = float.MinValue;
                            foreach (var edgeij in neighbors) {
                                if (!processedVertices.Contains(edgeij.To)) { //calculate transition probability
                                                                              //Lij - distance btw i-j
                                                                              //rij - ant pheromones btw i-j 
                                    var Lij = edgeij.Value;
                                    var nij = CaclulateMoveAttractiveness(Lij) * magnificationFactor;
                                    var rij = pheromones[edgeij.From.Index, edgeij.To.Index];
                                    var Pij = (MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa)) / SumiN;

                                    if (bestMove < Pij) {
                                        bestMove = Pij;
                                        bestEdgeToMove = edgeij;
                                    }
                                }
                            }
                            #endregion

                            break;
                        case ACOTypes.ACS:

                            #region Ant Colony System

                            //choice of next step based on a random generation
                            //first prepare percentage chance of each possible passes 
                            var PiN = new List<Tuple<float, GraphEdge>>();
                            var checkSumm = 0f;
                            foreach (var edgeij in neighbors) {
                                if (!processedVertices.Contains(edgeij.To)) {
                                    //calculate transition probability
                                    //Lij - distance btw i-j
                                    //rij - ant feromons btw i-j 
                                    var Lij = edgeij.Value;
                                    var nij = CaclulateMoveAttractiveness(Lij) * magnificationFactor;
                                    var rij = pheromones[edgeij.From.Index, edgeij.To.Index];
                                    var Pij =
                                        ((MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa)) / SumiN)
                                        * 100f;// multiply by 100 to make range 0-100
                                    checkSumm += Pij;
                                    PiN.Add(Tuple.Create(Pij, edgeij));
                                }
                            }

                            if (MathF.Abs(checkSumm - 100f) > 0.01f) {
                                throw new Exception("Sum of all changes must be equal 100%");
                            }

                            //generate random value 0 - 100%
                            var hit = random.Next(0, 100);
                            var prev = 0f;
                            foreach (var chance in PiN) {
                                //search result which hits in range
                                if (prev <= hit && hit < (prev + chance.Item1)) {
                                    bestEdgeToMove = chance.Item2;
                                    var i = bestEdgeToMove.From.Index;
                                    var j = bestEdgeToMove.To.Index;

                                    //the local pheromone update.

                                    //initial value of the pheromone
                                    var r0 = CaclulateMoveAttractiveness(bestEdgeToMove.Value) * magnificationFactor;
                                    var rij = pheromones[i, j];
                                    pheromones[i, j] = (1f - p) * rij + p * r0;

                                    break;
                                }
                                prev += chance.Item1;
                            }

                            #endregion

                            break;
                    }

                    if (!bestEdgeToMove.IsValid) {
                        throw new Exception("Not found any next vertex.");
                    }
                    //upadate info of current path
                    pathEdges.Add(new Edge(bestEdgeToMove.From.Index, bestEdgeToMove.To.Index));//keep passed edges
                    next = bestEdgeToMove.To;
                    pathLength += bestEdgeToMove.Value;
                }

                if (pathLength == 0 || pathVertices.Count < graph.Count) {
                    throw new Exception("Invalid path, lenght is zero or not all vertices were passed.");
                }

                //pheromone depends on the length of the chosen path:
                //the shorter the path, the higher the amount of added pheromone
                var dr = (settings.DistanceInfluenceOnPheromoneFactor / pathLength) * magnificationFactor;

                switch (settings.Type) {
                    case ACOTypes.AS:

                        #region  Ant System
                        if (minPathLength > pathLength) {
                            minPathLength = pathLength;
                            shortestPath = pathVertices;
                        }

                        //loop over all edges
                        for (int i = 0; i < edges.Length; i++) {
                            var edge = edges[i];
                            var key = new Edge(edge.From.Index, edge.To.Index);

                            //new pheromone rate
                            var drij = pathEdges.Contains(key) ?
                                dr  //if edge was used, apply new feromon value
                                : 0f;//only evaporation will effect on pheromone

                            var rij = pheromones[key.IndexFrom, key.IndexTo];

                            //update pheromones 
                            pheromones[key.IndexFrom, key.IndexTo] =
                                (1f - p) * rij // evaporation
                                + drij; // increase value of feromons based on found path

                        }
                        #endregion

                        break;
                    case ACOTypes.ACS:

                        #region Ant Colony System
                        if (minPathLength > pathLength) {
                            minPathLength = pathLength;
                            shortestPath = pathVertices;

                            //loop over all edges
                            for (int i = 0; i < edges.Length; i++) {
                                var edge = edges[i];
                                var key = new Edge(edge.From.Index, edge.To.Index);

                                //new pheromone rate
                                var drij = pathEdges.Contains(key) ?
                                    dr  //if edge was used, apply new feromon value
                                    : 0f;//only evaporation will effect on pheromone

                                var rij = pheromones[key.IndexFrom, key.IndexTo];

                                //update pheromones 
                                pheromones[key.IndexFrom, key.IndexTo] =
                                    (1f - p) * rij // evaporation
                                    + p * drij; // increase value of feromons based on found path

                            }
                        }
                        #endregion

                        break;
                }
            }

            return new GraphPath(shortestPath, minPathLength);
        }


        /// <summary>
        /// just a help fuction to calculate scalar depending on length
        /// calls as 'a priori knowledge'
        /// </summary>
        /// <param name="Lij"></param>
        /// <returns></returns>
        float CaclulateMoveAttractiveness(float Lij) {
            //Lij - distance btw i-j

            return settings.DistanceInfluenceFactor / Lij;


        }
    }
}


