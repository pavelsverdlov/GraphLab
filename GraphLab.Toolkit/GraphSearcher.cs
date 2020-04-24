using System;
using System.Collections.Generic;
using System.Text;

namespace GraphLab.Toolkit {
    public abstract class GraphSearcher {
        public abstract List<GraphVertex> FindPath();
    }
}
