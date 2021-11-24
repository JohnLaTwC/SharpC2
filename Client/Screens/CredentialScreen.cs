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
    public class CredentialScreen : Screen
    {
        protected override string ScreenName { get; set; } = "credentials";

        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        private List<CredentialRecord> _credentials;

        public CredentialScreen(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            
            Commands.Add(new GenericCommand("list", "List Credentials", ListCredentials));
            Commands.Add(new AddCredential(AddCredential));
            Commands.Add(new DeleteCredential(DeleteCredential));
            Commands.Add(new BackScreen(StopScreen));

            _credentials = _apiService.GetCredentials().GetAwaiter().GetResult().ToList();
        }

        protected override Task<int> OpenWindowCallback(string input, int caret)
        {
            // if input text is an integer, some guids will begin with an int
            return char.IsNumber(input[^1])
                ? Task.FromResult(1)
                : base.OpenWindowCallback(input, caret);
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
            
            // if command has no args, ignore
            if (command.Arguments is null || command.Arguments.Count == 0)
                return base.GetAutoComplete(input, caret);

            CompletionItem[] result = null;
            
            // return arguments based on split length
            var argPosition = splitInput.Length - 2;
            if (argPosition > command.Arguments.Count)
                return base.GetAutoComplete(input, caret);
                
            var argument = command.Arguments[argPosition];
            
            if (argument.Name.Equals("credential", StringComparison.OrdinalIgnoreCase))
            {
                result = _credentials?.Where(c => c.Guid.StartsWith(typedWord, StringComparison.OrdinalIgnoreCase))
                    .Select(c => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = c.Guid,
                        DisplayText = c.Guid,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult($"{c.Domain}\\{c.Username} ({c.Source})"))
                    }).ToArray();
            }
            
            return result is null
                ? Task.FromResult<IReadOnlyList<CompletionItem>>(Array.Empty<CompletionItem>())
                : Task.FromResult<IReadOnlyList<CompletionItem>>(result);
        }

        private async Task ListCredentials(string[] args)
        {
            _credentials = (await _apiService.GetCredentials()).ToList();

            var tmpList = new ResultList<CredentialRecord>();
            tmpList.AddRange(_credentials);

            if (!tmpList.Any())
            {
                Console.PrintOutput("No credentials.");
                return;
            }
            
            Console.PrintOutput(tmpList.ToString());
        }

        private async Task AddCredential(string[] args)
        {
            var success = false;
            
            switch (args.Length)
            {
                case 3:
                    success = await _apiService.AddCredential(args[1], args[2]);
                    break;
                
                case 4:
                    success = await _apiService.AddCredential(args[1], args[2], args[3]);
                    break;
                
                case 5:
                    success = await _apiService.AddCredential(args[1], args[2], args[3], args[4]);
                    break;
                
                default:
                    Console.PrintError("Incorrect number of args");
                    break;
            }
            
            if (success)
                Console.PrintSuccess("Credential added");
        }

        private async Task DeleteCredential(string[] args)
        {
            var guid = args[1];
            if (!_credentials.Any(c => c.Guid.Equals(guid)))
            {
                Console.PrintError("Unknown credential.");
                return;
            }
            
            if (await _apiService.DeleteCredential(guid))
                Console.PrintSuccess("Credential deleted.");
        }
    }
}