using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class AddHostedFile : ScreenCommand
    {
        public AddHostedFile(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "add";
        public override string Description => "Host the given file on the HTTP Handler";

        public override List<Argument> Arguments => new List<Argument>
        {
            new()
            {
                Name = "filename",
                Optional = false
            },
            new()
            {
                Name = "path",
                Optional = false,
                Artefact = true
            }
        };
        
        public override Screen.Callback Execute { get; }
    }
}