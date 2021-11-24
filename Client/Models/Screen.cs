using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PrettyPrompt;
using PrettyPrompt.Completion;
using PrettyPrompt.Consoles;

using SharpC2.ScreenCommands;

namespace SharpC2.Models
{
    public abstract class Screen
    {
        public SystemConsole Console { get; } = new();
        public List<ScreenCommand> Commands { get; } = new();
        
        protected abstract string ScreenName { get; set; }

        public delegate Task Callback(string[] args);
        
        private bool _screenRunning;
        private IPrompt _prompt;

        protected Screen()
        {
            Commands.Add(new PrintHelp(PrintHelp));
        }

        public async Task Show()
        {
            _prompt = new Prompt(null, new PromptCallbacks
            {
                CompletionCallback = GetAutoComplete,
                OpenCompletionWindowCallback = OpenWindowCallback
            });
            
            _screenRunning = true;

            while (_screenRunning)
            {
                var (isSuccess, text, _) = await _prompt.ReadLineAsync($"[{ScreenName}] > ");
                
                if (!isSuccess) continue;
                if (string.IsNullOrEmpty(text)) continue;
                
                var args = text.Split(" ");
                var command = Commands.FirstOrDefault(c =>
                    c.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));

                if (command is null)
                {
                    Console.PrintError("Unknown command");
                    continue;
                }

                // check mandatory arg length
                var commandArgs = args[1..];
                
                if (commandArgs.Length < command.Arguments?.Where(a => !a.Optional).Count())
                {
                    Console.PrintError("Not enough arguments");
                    Console.PrintOutput(command.Usage);
                    continue;
                }
                
                await command.Execute(args);
            }
        }

        protected virtual Task<int> OpenWindowCallback(string input, int caret)
        {
            if (caret == 1 && !char.IsWhiteSpace(input[0]) // 1 word character typed in brand new prompt
                           && (input.Length == 1 || !char.IsLetterOrDigit(input[1]))) // if there's more than one character on the prompt, but we're typing a new word at the beginning (e.g. "a| bar")
            {
                return Task.FromResult(1);
            }

            // open when we're starting a new "word" in the prompt.
            return caret - 2 >= 0
                   && char.IsWhiteSpace(input[caret - 2])
                   && char.IsLetter(input[caret - 1])
                ? Task.FromResult(1)
                : Task.FromResult(-1);
        }

        protected virtual Task<IReadOnlyList<CompletionItem>> GetAutoComplete(string input, int caret)
        {
            var textUntilCaret = input[..caret];
            var previousWordStart = textUntilCaret.LastIndexOf(' ');
            var typedWord = previousWordStart == -1
                ? textUntilCaret
                : textUntilCaret[(previousWordStart + 1)..];

            var result = Commands
                .Where(c => c.Name.StartsWith(typedWord))
                .Select(c => new CompletionItem
                {
                    StartIndex = previousWordStart + 1,
                    ReplacementText = c.Name,
                    DisplayText = c.Name,
                    ExtendedDescription = new Lazy<Task<string>>(() =>
                        Task.FromResult($"{c.Description}{Environment.NewLine}{c.Usage}"))
                })
                .ToArray();

            return Task.FromResult<IReadOnlyList<CompletionItem>>(result);
        }

        private Task PrintHelp(string[] args)
        {
            var list = new ResultList<ScreenCommand>();
            list.AddRange(Commands.OrderBy(c => c.Name));

            Console.PrintOutput(list.ToString());
            
            return Task.CompletedTask;
        }

        protected virtual Task StopScreen(string[] args)
        {
            _screenRunning = false;
            return Task.CompletedTask;
        }

        public virtual Task SetScreenName(string name)
        {
            ScreenName = name;
            return Task.CompletedTask;
        }
    }
}