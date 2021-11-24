using System.Collections.Generic;
using SharpC2.Models;

namespace SharpC2.ScreenCommands
{
    public class AddCredential : ScreenCommand
    {
        public AddCredential(Screen.Callback callback)
        {
            Execute = callback;
        }
        
        public override string Name => "add";
        public override string Description => "Add a credential";

        public override List<Argument> Arguments => new()
        {
            new Argument { Name = "username", Optional = false },
            new Argument { Name = "password", Optional = false },
            new Argument { Name = "domain", Optional = true },
            new Argument { Name = "source", Optional = true }
        };
        
        public override Screen.Callback Execute { get; }
    }
}