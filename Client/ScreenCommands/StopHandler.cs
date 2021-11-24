using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class StopHandler : ScreenCommand
    {
        public StopHandler(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "stop";
        public override string Description => "Stop a Handler";

        public override List<Argument> Arguments => new List<Argument>
        {
            new()
            {
                Name = "handler",
                Optional = false
            }
        };
        
        public override Screen.Callback Execute { get; }
    }
}