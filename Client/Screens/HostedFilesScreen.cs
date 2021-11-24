using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrettyPrompt.Completion;
using SharpC2.Models;
using SharpC2.ScreenCommands;
using SharpC2.Services;

namespace SharpC2.Screens
{
    public class HostedFilesScreen : Screen, IDisposable
    {
        protected override string ScreenName { get; set; } = "hosted-files";

        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        private List<HostedFile> _hostedFiles;

        public HostedFilesScreen(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;

            Commands.Add(new GenericCommand("list", "List hosted files", ListHostedFiles));
            Commands.Add(new AddHostedFile(AddHostedFile));
            Commands.Add(new DeleteHostedFile(DeleteHostedFile));
            Commands.Add(new BackScreen(StopScreen));

            _hostedFiles = _apiService.GetHostedFiles().GetAwaiter().GetResult().ToList();

            _signalRService.HostedFileAdded += OnHostedFileAdded;
            _signalRService.HostedFileDeleted += OnHostedFileDeleted;
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

            if (argument.Name.Equals("filename", StringComparison.OrdinalIgnoreCase))
            {
                result = _hostedFiles.Where(f => f.Filename.StartsWith(typedWord))
                    .Select(f => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = f.Filename,
                        DisplayText = f.Filename,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult($"{f.Size} bytes."))
                    }).ToArray();
            }
            else if (argument.Name.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                IEnumerable<string> fileSystemEntries;

                // suppress exceptions
                try { fileSystemEntries = Directory.EnumerateFileSystemEntries(typedWord); }
                catch { fileSystemEntries = Array.Empty<string>(); }
                
                result = fileSystemEntries
                    .Select(path => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = path,
                        DisplayText = path,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult(""))
                    }).ToArray();
            }
            
            return result is null
                ? Task.FromResult<IReadOnlyList<CompletionItem>>(Array.Empty<CompletionItem>())
                : Task.FromResult<IReadOnlyList<CompletionItem>>(result);
        }

        protected override Task<int> OpenWindowCallback(string input, int caret)
        {
            return input.EndsWith('\\')
                ? Task.FromResult(1)
                : base.OpenWindowCallback(input, caret);
        }

        private async Task ListHostedFiles(string[] args)
        {
            _hostedFiles = (await _apiService.GetHostedFiles()).ToList();

            if (!_hostedFiles.Any())
            {
                Console.PrintOutput("No hosted files");
                return;
            }

            var list = new ResultList<Result>();
            list.AddRange(_hostedFiles);
            
            Console.PrintOutput(list.ToString());
        }

        private async Task AddHostedFile(string[] args)
        {
            var filename = args[1];
            var path = args[2];

            if (!File.Exists(path))
            {
                Console.PrintError("File not found");
                return;
            }

            var content = await File.ReadAllBytesAsync(path);
            await _apiService.AddHostedFile(filename, content);
        }

        private async Task DeleteHostedFile(string[] args)
        {
            var filename = args[1];
            await _apiService.DeleteHostedFile(filename);
        }
        
        private void OnHostedFileAdded(string filename)
            => Console.PrintSuccess($"{filename} hosted.");

        private void OnHostedFileDeleted(string filename)
            => Console.PrintSuccess($"{filename} removed.");

        public void Dispose()
        {
            _signalRService.HostedFileAdded -= OnHostedFileAdded;
            _signalRService.HostedFileDeleted -= OnHostedFileDeleted;
        }
    }
}