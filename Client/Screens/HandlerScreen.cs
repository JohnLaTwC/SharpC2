using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using PrettyPrompt.Completion;

using SharpC2.Models;
using SharpC2.ScreenCommands;
using SharpC2.Services;

namespace SharpC2.Screens
{
    public class HandlerScreen : Screen, IDisposable
    {
        protected override string ScreenName { get; set; } = "handlers";

        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        private List<Handler> _handlers;

        public HandlerScreen(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;

            Commands.Add(new GenericCommand("list", "List Handlers", ListHandlers));
            Commands.Add(new SetHandler(SetHandlerParameter));
            Commands.Add(new StartHandler(StartHandler));
            Commands.Add(new StopHandler(StopHandler));
            Commands.Add(new BackScreen(StopScreen));

            _handlers = _apiService.GetHandlers().GetAwaiter().GetResult().ToList();

            _signalRService.HandlerParameterSet += OnHandlerParameterSet;
            _signalRService.HandlerStarted += OnHandlerStarted;
            _signalRService.HandlerStopped += OnHandlerStopped;
        }

        protected override Task<IReadOnlyList<CompletionItem>> GetAutoComplete(string input, int caret)
        {
            var textUntilCaret = input[..caret];
            var previousWordStart = textUntilCaret.LastIndexOf(' ');
            var typedWord = previousWordStart == -1
                ? textUntilCaret.ToLower()
                : textUntilCaret[(previousWordStart + 1)..].ToLower();
            
            // split the input
            var splitInput = input.Split(' ');
            
            // if there's only 1 element in the split, return it
            if (splitInput.Length == 1)
                return base.GetAutoComplete(input, caret);
            
            // otherwise, get an instance of the command
            var firstWord = input.Split(' ')[0];
            var command = Commands.FirstOrDefault(c => c.Name.Equals(firstWord));
            
            // not sure this should happen, but meh
            if (command is null)
                return base.GetAutoComplete(input, caret);
            
            // return arguments based on split length
            var argPosition = splitInput.Length - 2;
            if (argPosition > command.Arguments.Count)
                return base.GetAutoComplete(input, caret);
                
            var argument = command.Arguments[argPosition];
            
            CompletionItem[] result = null;

            if (argument.Name.Equals("handler", StringComparison.OrdinalIgnoreCase))
            {
                result = _handlers.Where(h => h.Name.StartsWith(typedWord))
                    .Select(h => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = h.Name,
                        DisplayText = h.Name,
                        ExtendedDescription = new Lazy<Task<string>>(() =>
                            Task.FromResult($"{h.ParametersAsString}{Environment.NewLine}Running: {h.Running}"))
                    }).ToArray();
            }
            else if (argument.Name.Equals("parameter", StringComparison.OrdinalIgnoreCase))
            {
                var handlerName = splitInput[1];
                var handler = _handlers.FirstOrDefault(h => h.Name.Equals(handlerName));
                
                result = handler?.Parameters.Where(p => p.Name.StartsWith(typedWord, StringComparison.OrdinalIgnoreCase))
                    .Select(p => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = p.Name,
                        DisplayText = p.Name,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult($" Current value: {p.Value}"))
                    }).ToArray();
            }

            return result is null
                ? Task.FromResult<IReadOnlyList<CompletionItem>>(Array.Empty<CompletionItem>())
                : Task.FromResult<IReadOnlyList<CompletionItem>>(result);
        }

        private async Task ListHandlers(string[] args)
        {
            _handlers = (await _apiService.GetHandlers()).ToList();

            var list = new ResultList<Handler>();
            list.AddRange(_handlers);
            
            Console.PrintOutput(list.ToString());
        }

        private async Task SetHandlerParameter(string[] args)
        {
            var handlerName = args[1];
            var parameter = args[2];
            var value = args[3];

            var handler = await _apiService.SetHandlerParameter(handlerName, parameter, value);
            var index = _handlers.FindIndex(h => h.Name.Equals(handler.Name));
            _handlers[index] = handler;
        }

        private async Task StartHandler(string[] args)
            => await _apiService.StartHandler(args[1]);

        private async Task StopHandler(string[] args)
            => await _apiService.StopHandler(args[1]);

        private void OnHandlerStarted(Handler handler)
            => Console.PrintSuccess($"Handler \"{handler.Name}\" started.");

        private void OnHandlerStopped(Handler handler)
            => Console.PrintSuccess($"Handler \"{handler.Name}\" stopped.");

        private void OnHandlerParameterSet(string key, string value)
            => Console.PrintSuccess($"{key} set to {value}");

        public void Dispose()
        {
            _signalRService.HandlerParameterSet -= OnHandlerParameterSet;
            _signalRService.HandlerStarted -= OnHandlerStarted;
            _signalRService.HandlerStopped -= OnHandlerStopped;
        }
    }
}