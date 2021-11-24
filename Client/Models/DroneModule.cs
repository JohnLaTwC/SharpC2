using System.Collections.Generic;

namespace SharpC2.Models
{
    public class DroneModule
    {
        public string Name { get; set; }
        public IEnumerable<Command> Commands { get; set; }

        public class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public IEnumerable<Argument> Arguments { get; set; }
            
            public class Argument
            {
                public string Label { get; set; }
                public bool Artefact { get; set; }
                public bool Optional { get; set; }
            }
        }
    }
}