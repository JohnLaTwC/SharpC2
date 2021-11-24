using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class ListDrones : ScreenCommand
    {
        public ListDrones(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "list";
        public override string Description => "List Drones";
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}