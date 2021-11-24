using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class DeleteHostedFile : ScreenCommand
    {
        public DeleteHostedFile(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "remove";
        public override string Description => "Remove a hosted file";

        public override List<Argument> Arguments => new()
        {
            new Argument
            {
                Name = "filename",
                Optional = false
            }
        };
        
        public override Screen.Callback Execute { get; }
    }
}