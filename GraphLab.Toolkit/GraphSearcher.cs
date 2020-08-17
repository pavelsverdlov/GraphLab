using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLab.Toolkit {
    public enum StartVertexOptions {
        AllVertices,
        RandomVertex,
    }
    public abstract class GraphSearcher {

        public static GraphSearcher CreateACO(GraphStructure graph, Search.ACOSettings settings) 
            => new Search.ACO(graph, settings);

        public abstract GraphPath FindPath(StartVertexOptions option);
        public abstract GraphPath FindPath(GraphVertex start);
    }
}
