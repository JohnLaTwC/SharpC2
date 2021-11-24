using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class StartHandler : ScreenCommand
    {
        public StartHandler(Screen.Callback callback)
        {
            Execute = callback;
        }

        public override string Name => "start";
        public override string Description => "Start Handler";

        public override List<Argument> Arguments => new()
        {
            new Argument
            {
                Name = "handler",
                Optional = false
            }
        };

        public override Screen.Callback Execute { get; }
    }
}