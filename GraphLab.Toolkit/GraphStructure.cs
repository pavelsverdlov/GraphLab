﻿using MathNet.Numerics.LinearAlgebra;

using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLab.Toolkit {
    //https://codereview.stackexchange.com/questions/131583/generic-graph-implementation-in-c

    public readonly struct GraphVertex : IEquatable<GraphVertex> {
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
        public override string ToString() => $"{Index}";
    }
    public readonly struct GraphEdge {
        public readonly bool IsValid;
        public readonly GraphVertex From;
        public readonly GraphVertex To;
        public readonly float Value;

        internal GraphEdge(GraphVertex v0, GraphVertex graphVertex, float v) {
            this.From = v0;
            this.To = graphVertex;
            this.Value = v;
            IsValid = true;
        }

        public override string ToString() => $"[{From}-{To}:{Value}]";
    }
    public struct GraphEccentricity {
        public GraphVertex Center;
        public GraphVertex Border;
    }

    public class GraphStructure {
        /// <summary>
        /// Set of vertices (also called nodes or points);
        /// </summary>
        public int Count { get; }

        readonly Matrix<float> adjacencyMatrix;
        public GraphStructure(int vertexCount) {
            if(vertexCount <= 0) {
                throw new ArgumentException("vertexCount must be more than 0.");
            }
            adjacencyMatrix = Matrix<float>.Build.Dense(vertexCount, vertexCount, 0f);
            Count = vertexCount;
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

        public GraphEdge[] GetEdges() {
            var endges = new List<GraphEdge>();
            for (var row = 0; row < adjacencyMatrix.RowCount; row++) {
                for (var col = 0; col < adjacencyMatrix.ColumnCount; col++) {
                    var r = adjacencyMatrix[row, col];
                    endges.Add(new GraphEdge(new GraphVertex(row),new GraphVertex(col),r));
                }
            }
            return endges.ToArray();
        }

        public IEnumerable<GraphEdge> GetNeighbors(GraphVertex v0) {
            var neighbors = new List<GraphEdge>();
            for (var i = 0; i < adjacencyMatrix.ColumnCount; ++i) {
                neighbors.Add(new GraphEdge(v0, new GraphVertex(i), adjacencyMatrix[v0.Index, i]));
            }
            return neighbors;
        }
        public GraphEdge GetEdge(GraphVertex v0, GraphVertex v1) {
            return new GraphEdge(v0, new GraphVertex(v1.Index), adjacencyMatrix[v0.Index, v1.Index]);
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
            
            return new GraphEccentricity { Border = new GraphVertex(maxRowIndex), Center = new GraphVertex(minRowIndex) };
        }
    }


    public class GraphPath {
        public IReadOnlyList<GraphVertex> Vertices { get; }
        public float Lenght { get; }

        public GraphPath(List<GraphVertex> path, float lenght) {
            Vertices = path;
            Lenght = lenght;
        }

    }
}
