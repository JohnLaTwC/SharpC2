using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class DeleteCredential : ScreenCommand
    {
        public DeleteCredential(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "delete";
        public override string Description => "Delete the given credential";

        public override List<Argument> Arguments => new List<Argument>
        {
            new() { Name = "credential" }
        };
        
        public override Screen.Callback Execute { get; }
    }
}