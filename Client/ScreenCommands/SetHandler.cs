using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class SetHandler : ScreenCommand
    {
        public SetHandler(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "set";
        public override string Description => "Set a Handler option";

        public override List<Argument> Arguments => new List<Argument>
        {
            new() { Name = "handler", Optional = false },
            new() { Name = "parameter", Optional = false },
            new() { Name = "value", Optional = false }
        };
        
        public override Screen.Callback Execute { get; }
    }
}