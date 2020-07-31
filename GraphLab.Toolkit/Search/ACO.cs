using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GraphLab.Toolkit.Search {

    public struct ACOSettings {
        public static ACOSettings Default() => new ACOSettings {
            Alfa = 1,
            Beta = 1,
            Itreations = 1000,
            Q = 1
        };
        // >=0
        public float Alfa { get; set; }
        //>=1
        public float Beta { get; set; }
        public int Itreations { get; set; }
        /// <summary>
        /// constant uses for calculation new pferomon factor (Q/L)
        /// L - path lenght
        /// </summary>
        public float Q { get; set; }

    }

    /// <summary>
    /// Ant colony optimization algorithms
    /// https://en.wikipedia.org/wiki/Ant_colony_optimization_algorithms
    /// </summary>
    public class ACO : GraphSearcher {
        readonly Dictionary<Tuple<int, int>, float> pheromone = new Dictionary<Tuple<int, int>, float>();
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
            return FindPath(g=>g.GetVertex(randomStart.Next(0, g.V)));
        }
        GraphPath FindPath(Func<GraphStructure, GraphVertex> getStartVertex) {
            var paths = new SortedList<float, List<GraphVertex>>();
            var minPathLenght = float.MaxValue;

            try {
                var path = new List<GraphVertex>();
                var processed = new HashSet<GraphVertex>();
                var random = new Random();
                var Q = 1f;
                var pathLenght = 0f;
               
                var itreations = settings.Itreations;

                while (itreations-- > 1) {
                    pathLenght = 0f;
                    path.Clear();
                    processed.Clear();

                    var next = getStartVertex(graph);

                    while (next.IsValid) {
                        processed.Add(next);
                        path.Add(next);
                        var i = next;

                        float SumiN = 0;
                        var neighbors = graph.GetNeighbors(i);
                        foreach (var edge in neighbors) {
                            if (!processed.Contains(edge.To)) {
                                var Lij = edge.Value;
                                var nij = CaclulateSpecialScalar(Lij);
                                var rij = GetPheromone(edge);
                                SumiN += (float)(Math.Pow(nij, settings.Beta) * Math.Pow(rij, settings.Alfa));
                            }
                        }
                        var PiN = new List<Tuple<float, GraphEdge>>();
                        var checkSumm = 0f;
                        foreach (var edge in neighbors) {
                            if (!processed.Contains(edge.To)) {
                                //calculate transition probability
                                var Pij = 
                                    CalculateProbability(edge.Value, GetPheromone(edge), SumiN)
                                    * 100f;// multiply by 100 to make range 0-100
                                checkSumm += Pij;
                                PiN.Add(Tuple.Create(Pij, edge));
                            }
                        }
                        //Debug.Assert(checkSumm >= 100);
                        //generate random value 0 - 100% 
                        //this is range of Pij results
                        var chance = random.Next(0, 100);
                        var prev = 0f;
                        next = default;
                        foreach (var val in PiN) {
                            //search result which hits in range
                            if (prev <= chance && chance <= (prev + val.Item1)) {
                                next = val.Item2.To;
                                pathLenght += val.Item2.Value;
                                break;
                            }
                            prev += val.Item1;
                        }
                        if (path.Count == graph.V) {
                            break;//all nodes were passed
                        }
                    }
                    if (paths.ContainsKey(pathLenght)) {
                        paths[pathLenght] = path;
                    } else {
                        paths.Add(pathLenght, path);
                    }

                    if (minPathLenght > pathLenght) {
                        minPathLenght = pathLenght;
                    }

                    //new coef of feromons
                    //var dr = (path.Count / pathLenght) * 100;
                    var dr = Q / pathLenght;// CalculateNewPheromoneFactor(pathLenght, minPathLenght);

                    var pi = path[0];
                    for (var n = 1; n < path.Count; n++) {
                        var pj = path[n];
                        var key = Tuple.Create(pi.Index, pj.Index);

                        //coef of forgetting previous feromons, depends on itreations 
                        //on the first iteration forgetting more than in the last iterations
                        var forgetFactor = 1f / itreations;

                        pheromone[key] =
                            (1f - forgetFactor) * pheromone[key] 
                            + dr; // increse value of feromons for found path

                        Debug.Assert(!float.IsInfinity(pheromone[key]));

                        pi = pj;
                    }
                }
            } catch(Exception ex) {
                ex.ToString();
            }
            return new GraphPath(paths[minPathLenght], minPathLenght);
        }

        float GetPheromone(GraphEdge edge) {
            var key = Tuple.Create(edge.From.Index, edge.To.Index);
            if (!pheromone.ContainsKey(key)) {
               // pheromone[key] = CaclulateSpecialScalar(nodesDictionary[i].Distances[j]);
                pheromone[key] = CaclulateSpecialScalar(edge.Value);
            }
            return pheromone[key];
        }
        float CalculateProbability(float Lij, float rij, float SumiN) {
            //Lij - distance btw i-j
            //rij - ant feromons btw i-j 
            //Sumij - summ each ditances btw i-(N) N each vertext connected to i
            var nij = CaclulateSpecialScalar(Lij);

            return (MathF.Pow(nij, settings.Beta) * MathF.Pow(rij, settings.Alfa)) / SumiN;
        }
        /// <summary>
        /// just a help fuction to calculate scalar depending on length
        /// calls as 'a priori knowledge'
        /// </summary>
        /// <param name="Lij"></param>
        /// <returns></returns>
        static float CaclulateSpecialScalar(float Lij) {
            //Lij - distance btw i-j

            return (1f / Lij);
        }

        static float CalculateNewPheromoneFactor(float pathLenght, float minPathLenght) {
            return pathLenght / minPathLenght * 2; //make it depends on min path
        }


    }
}
