using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class ExitClient : ScreenCommand
    {
        public ExitClient(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "exit";
        public override string Description => "Exit this client";
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}