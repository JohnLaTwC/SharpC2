using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class GenericCommand : ScreenCommand
    {
        public GenericCommand(string name, string description, Screen.Callback callback, List<Argument> arguments = null)
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