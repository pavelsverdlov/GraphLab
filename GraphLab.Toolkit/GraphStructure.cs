using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLab.Toolkit {
    //https://codereview.stackexchange.com/questions/131583/generic-graph-implementation-in-c

    public struct GraphVertex : IEquatable<GraphVertex> {
        public readonly bool IsValid;
        public readonly int Index;

        internal GraphVertex(int i) {
            IsValid = true;
            Index = i;
        }

        public override bool Equals(object obj) {
            return obj is GraphVertex vertex && Equals(vertex);
        }

        public bool Equals(GraphVertex other) {
            return IsValid == other.IsValid &&
                   Index == other.Index;
        }

        public override int GetHashCode() {
            return HashCode.Combine(IsValid, Index);
        }
    }
    public struct GraphEdge {
        public readonly GraphVertex From;
        public readonly GraphVertex To;
        public readonly float Value;

        internal GraphEdge(GraphVertex v0, GraphVertex graphVertex, float v) {
            this.From = v0;
            this.To = graphVertex;
            this.Value = v;
        }
    }
    public struct GraphEccentricity {
        public int CenterIndx;
        public int BorderIndx;
    }

    public class GraphStructure {
        /// <summary>
        /// V a set of vertices (also called nodes or points);
        /// </summary>
        public int V { get; }
        /// <summary>
        /// E a set of edges (also called directed edges, directed links, directed lines, arrows or arcs);
        /// </summary>
        public int E { get; }

        readonly Matrix<float> adjacencyMatrix;
        readonly List<GraphVertex> vertices;
        public GraphStructure(int vertexCount) {
            adjacencyMatrix = Matrix<float>.Build.Dense(vertexCount, vertexCount, 0f);
            vertices = new List<GraphVertex>();
            V = vertexCount;
        }

        public void AddEdge(int v0, int v1, float val) {
            adjacencyMatrix[v0, v1] = val;
        }

        public float GetVal(int v0, int v1) {
            return adjacencyMatrix[v0, v1];
        }

        public GraphVertex GetVertex(int v) {
            return new GraphVertex(v);
        }

        public IEnumerable<GraphEdge> GetNeighbors(GraphVertex v0) {
            var neighbors = new List<GraphEdge>();
            for (var i =0; i < adjacencyMatrix.ColumnCount; ++i) {
                neighbors.Add(new GraphEdge(v0, new GraphVertex(i), adjacencyMatrix[v0.Index,i]));
            }
            return neighbors;
        }

        public GraphEccentricity CalculateEccentricity() {
            var minRowIndex = -1;
            var maxRowIndex = -1;
            var max = float.MinValue;
            var min = float.MaxValue;

            for (var row = 0; row < adjacencyMatrix.RowCount; row++) {
                var rmax = float.MinValue;
                for (var col = 0; col < adjacencyMatrix.ColumnCount; col++) {
                    var r = adjacencyMatrix[row, col];
                    if (r > rmax) {
                        rmax = r;
                    }
                }

                if (rmax > max) {
                    max = rmax;
                    maxRowIndex = row;
                }
                if (min > rmax) {
                    min = rmax;
                    minRowIndex = row;
                }
            }

            return new GraphEccentricity { BorderIndx = maxRowIndex, CenterIndx = minRowIndex };
        }
    }
}
