using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using WPFLab;

namespace GraphLab.Viewer {
    class GraphLabMapperService : MapperService {
        protected override void RegisterMaping(IMapperConfigurationExpression cfg) {
            cfg.CreateMap<GraphPropertiesProxy, Toolkit.Search.ACOSettings>();
            cfg.CreateMap<Toolkit.Search.ACOSettings, GraphPropertiesProxy>();
        }
    }
}
