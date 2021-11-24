using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class DroneCommand : ScreenCommand
    {
        public DroneCommand(string name, string description, List<Argument> arguments, Screen.Callback callback)
        {
            Name = name;
            Description = description;
            Arguments = arguments;
            Execute = callback;
        }
        
        public override string Name { get; }
        public override string Description { get; }
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}