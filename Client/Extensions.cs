using System;

using PrettyPrompt.Consoles;

namespace SharpC2
{
    public static class Extensions
    {
        public static void PrintOutput(this IConsole console, string output)
        {
            console.WriteLine($"{Environment.NewLine}{output}{Environment.NewLine}");
        }
        
        public static void PrintSuccess(this IConsole console, string message)
        {
            var currentColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            console.WriteLine($"[+] {message}");
            Console.ForegroundColor = currentColour;
        }
        
        public static void PrintWarning(this IConsole console, string warning)
        {
            var currentColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            console.WriteLine($"[!] {warning}");
            Console.ForegroundColor = currentColour;
        }
        
        public static void PrintError(this IConsole console, string error)
        {
            var currentColour = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            console.WriteLine($"[x] {error}");
            Console.ForegroundColor = currentColour;
        }
    }
}