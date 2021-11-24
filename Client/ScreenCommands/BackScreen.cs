using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class BackScreen : ScreenCommand
    {
        public BackScreen(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "back";
        public override string Description => "Go back to the previous screen";
        public override List<Argument> Arguments { get; }
        public override Screen.Callback Execute { get; }
    }
}