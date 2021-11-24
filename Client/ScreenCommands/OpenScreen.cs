using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class OpenScreen : ScreenCommand
    {
        public OpenScreen(string name, string description, Screen.Callback callback, List<Argument> args = null)
        {
            Name = name;
            Description = description;
            Execute = callback;

            if (args is not null)
                Arguments = args;
        }
        
        public override string Name { get; }
        public override string Description { get; }
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}