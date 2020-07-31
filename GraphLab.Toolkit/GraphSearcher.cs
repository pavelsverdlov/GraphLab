using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLab.Toolkit {
    public abstract class GraphSearcher {
        public abstract GraphPath FindPath();
        public abstract GraphPath FindPath(GraphVertex start);
    }
}
