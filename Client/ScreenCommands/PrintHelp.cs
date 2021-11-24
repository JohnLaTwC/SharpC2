using System.Collections.Generic;

using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class PrintHelp : ScreenCommand
    {
        public PrintHelp(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "help";
        public override string Description => "Print a list of commands and their description";
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}