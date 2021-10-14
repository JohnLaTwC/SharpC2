using System;
using System.Threading.Tasks;

using SharpC2.Models;
using SharpC2.Screens;

namespace SharpC2.ScreenCommands.PayloadCommands
{
    public class SetPayloadOptionCommand : ScreenCommand
    {
        public override string Name => "set";
        public override string Description => "Set a payload option";
        public override string Usage => "set [option] [value]";
        public override Screen.CommandCallback Callback => SetPayloadOption;

        private readonly Screen _screen;

        public SetPayloadOptionCommand(Screen screen)
        {
            _screen = screen;
        }

        private Task SetPayloadOption(string[] args)
        {
            var screen = (PayloadsScreen)_screen;
            
            if (args.Length < 3)
            {
                screen.Console.PrintError("Not enough arguments");
                return Task.CompletedTask;
            }
            
            var option = args[1];

            if (option.Equals("handler", StringComparison.OrdinalIgnoreCase))
            {
                var handler = args[2];
                screen.Payload.Handler = handler;
            }
            else if (option.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                var format = StringToEnum(args[2]);
                screen.Payload.Format = format;
            }
            else
            {
                screen.Console.PrintError("Unknown payload option");
            }
            
            return Task.CompletedTask;
        }

        private static Payload.PayloadFormat StringToEnum(string value)
        {
            return !Enum.TryParse(value, out Payload.PayloadFormat format)
                ? Payload.PayloadFormat.Exe
                : format;
        }
    }
}