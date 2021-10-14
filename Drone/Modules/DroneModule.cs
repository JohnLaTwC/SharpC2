using System.Collections.Generic;

using MinHook;

namespace Drone.Modules
{
    public abstract class DroneModule
    {
        public abstract string Name { get; }
        public abstract List<Command> Commands { get; }

        protected Drone Drone;
        protected DroneConfig Config;
        protected HookEngine Hooks;

        public void Init(Drone drone, DroneConfig config, HookEngine hooks)
        {
            Drone = drone;
            Config = config;
            Hooks = hooks;
        }

        public class Command
        {
            public string Name { get; }
            public string Description { get; }
            public bool Visible { get; }
            public bool Hookable { get; }
            public List<Argument> Arguments { get; }
            public Drone.Callback Callback { get; }

            public Command(string name, string description, Drone.Callback callback, List<Argument> arguments = null, bool visible = true, bool hookable = false)
            {
                Name = name;
                Description = description;
                Callback = callback;
                Arguments = arguments ?? new List<Argument>();
                Visible = visible;
                Hookable = hookable;
            }

            public class Argument
            {
                public string Label { get; }
                public bool Artefact { get; }
                public bool Optional { get; }

                public Argument(string label, bool optional = true, bool artefact = false)
                {
                    Label = label;
                    Artefact = artefact;
                    Optional = optional;
                }
            }
        }
    }
}