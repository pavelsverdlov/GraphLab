using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GraphLab.Toolkit.Search {
    /// <summary>
    /// Ant colony optimization algorithms
    /// </summary>
    public class ACO : GraphSearcher {
        const float alfa = 3.5f;
        const float beta = 2f;
        readonly Dictionary<Tuple<int, int>, float> pheromone = new Dictionary<Tuple<int, int>, float>();
        readonly GraphStructure graph;

        public ACO(GraphStructure graph) {
            this.graph = graph;
        }

        public override List<GraphVertex> FindPath() {
            var paths = new SortedList<float, List<GraphVertex>>();
            var minPathLenght = float.MaxValue;

            try {
                var eccs = graph.CalculateEccentricity();

                var path = new List<GraphVertex>();
                var processed = new HashSet<GraphVertex>();
                var random = new Random();
                var Q = 1f;
                var pathLenght = 0f;
               
                var itreations = 100;

                while (itreations-- > 1) {
                    pathLenght = 0f;
                    path.Clear();
                    processed.Clear();

                    var next = graph.GetVertex(eccs.CenterIndx);

                    while (next.IsValid) {
                        processed.Add(next);
                        path.Add(next);
                        var i = next;

                        float SumiN = 0;
                        var neighbors = graph.GetNeighbors(i);
                        foreach (var edge in neighbors) {
                            if (!processed.Contains(edge.To)) {
                                var Lij = edge.Value;
                                SumiN += (float)(Math.Pow(CaclulateSpecialScalar(Lij), beta)
                                    * Math.Pow(GetPheromone(edge), alfa));
                            }
                        }
                        var PiN = new SortedList<float, GraphEdge>();
                        var checkSumm = 0f;
                        foreach (var edge in neighbors) {
                            if (!processed.Contains(edge.To)) {
                                //calculate transition probability
                                var Pij = CalculateProbability(edge.Value, GetPheromone(edge), SumiN);
                                checkSumm += Pij;
                                PiN.Add(Pij, edge);
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
                            if (prev <= chance && chance <= (prev + val.Key)) {
                                next = val.Value.To;
                                pathLenght += val.Value.Value;
                                break;
                            }
                            prev += val.Key;
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
                    var dr = CalculateNewPheromoneFactor(pathLenght, minPathLenght);

                    var pi = path[0];
                    for (var n = 1; n < path.Count; n++) {
                        var pj = path[n];
                        var key = Tuple.Create(pi.Index, pj.Index);
                        pheromone[key] =
                            //coef of forgetting previous feromons, depends on itreations 
                            //on the first iteration forgetting more than in the last iterations
                            (1f - 1f / itreations)
                            * pheromone[key] + dr; // increse value of feromons for found path
                        Debug.Assert(!float.IsInfinity(pheromone[key]));
                        pi = pj;
                    }
                }
            } catch(Exception ex) {
                ex.ToString();
            }
            return paths[minPathLenght];
        }

        float GetPheromone(GraphEdge edge) {
            var key = Tuple.Create(edge.From.Index, edge.To.Index);
            if (!pheromone.ContainsKey(key)) {
               // pheromone[key] = CaclulateSpecialScalar(nodesDictionary[i].Distances[j]);
                pheromone[key] = CaclulateSpecialScalar(edge.Value);
            }
            return pheromone[key];
        }
        static float CalculateProbability(float Lij, float rij, float SumiN) {
            //Lij - distance btw i-j
            //rij - ant feromons btw i-j 
            //Sumij - summ each ditances btw i-(N) N each vertext connected to i
            // var nij = 1f / Lij;

            return 100f * ((float)(Math.Pow(CaclulateSpecialScalar(Lij), beta) * Math.Pow(rij, alfa)) / SumiN);
        }
        /// <summary>
        /// just a help fuction to calculate scalar depending on length
        /// </summary>
        /// <param name="Lij"></param>
        /// <returns></returns>
        static float CaclulateSpecialScalar(float Lij) {
            //Lij - distance btw i-j

            return 100 * (1f / Lij);
        }

        static float CalculateNewPheromoneFactor(float pathLenght, float minPathLenght) {
            return pathLenght / minPathLenght * 2; //make it depends on min path
        }


    }
}
