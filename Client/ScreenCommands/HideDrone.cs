using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class HideDrone : ScreenCommand
    {
        public HideDrone(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "hide";
        public override string Description => "Hide an inactive Drone.";

        public override List<Argument> Arguments => new()
        {
            new Argument { Name = "drone", Optional = false }
        };
        
        public override Screen.Callback Execute { get; }
    }
}