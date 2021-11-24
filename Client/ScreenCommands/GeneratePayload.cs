using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class GeneratePayload : ScreenCommand
    {
        public GeneratePayload(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "payload";
        public override string Description => "Generate a payload";

        public override List<Argument> Arguments => new()
        {
            new Argument
            {
                Name = "handler",
                Optional = false
            },
            new Argument
            {
                Name = "format",
                Optional = false
            },
            new Argument
            {
                Name = "path",
                Artefact = true,
                Optional = false
            }
        };
        
        public override Screen.Callback Execute { get; }
    }
}