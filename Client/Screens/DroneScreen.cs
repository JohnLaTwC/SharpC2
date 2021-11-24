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
    public class DroneScreen : Screen, IDisposable
    {
        protected override string ScreenName { get; set; } = "drones";

        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;
        private readonly ScreenService _screenService;

        private readonly List<Handler> _handlers;
        private readonly IEnumerable<string> _payloadFormats;
        
        private readonly List<Drone> _drones;

        public DroneScreen(ApiService apiService, SignalRService signalRService, ScreenService screenService)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            _screenService = screenService;

            Commands.Add(new GenericCommand("list", "List Drones", ListDrones));
            Commands.Add(new OpenScreen("interact", "Interact with a Drone", InteractWithDrone, new List<ScreenCommand.Argument>
            {
                new() {Name = "drone", Optional = false}
            }));
            Commands.Add(new OpenScreen("handlers", "Manage Handlers", OpenHandlerScreen));
            Commands.Add(new OpenScreen("hosted-files", "Manage hosted files", OpenHostedFilesScreen));
            Commands.Add(new OpenScreen("credentials", "Manage credentials", OpenCredScreen));
            Commands.Add(new GeneratePayload(GeneratePayload));
            Commands.Add(new HideDrone(HideDrone));
            Commands.Add(new ExitClient(StopScreen));

            _handlers = _apiService.GetHandlers().GetAwaiter().GetResult().ToList();
            _drones = _apiService.GetDrones().GetAwaiter().GetResult().ToList();
            _payloadFormats = _apiService.GetPayloadFormats().GetAwaiter().GetResult();

            _signalRService.DroneCheckedIn += OnDroneCheckedIn;
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

            if (argument.Name.Equals("drone", StringComparison.OrdinalIgnoreCase))
            {
                result = _drones?.Where(d => d.Guid.StartsWith(typedWord, StringComparison.OrdinalIgnoreCase)
                    && !d.Hidden)
                    .Select(d => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = d.Guid,
                        DisplayText = d.Guid,
                        ExtendedDescription = new Lazy<Task<string>>(() =>
                            Task.FromResult($"{d.Username}@{d.Hostname}{Environment.NewLine}{d.Process} ({d.Pid})"))
                    }).ToArray();
            }
            else if (argument.Name.Equals("handler", StringComparison.OrdinalIgnoreCase))
            {
                result = _handlers.Where(h => h.Name.StartsWith(typedWord))
                    .Select(h => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = h.Name,
                        DisplayText = h.Name,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult(h.ParametersAsString))
                    }).ToArray();
            }
            else if (argument.Name.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                result = _payloadFormats
                    .Select(f => new CompletionItem
                    {
                        StartIndex = previousWordStart + 1,
                        ReplacementText = f,
                        DisplayText = f,
                        ExtendedDescription = new Lazy<Task<string>>(() => Task.FromResult(""))
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
            // if input text is an integer, some drone guids will begin with an int
            // or if char is a \
            return char.IsNumber(input[^1]) || input.EndsWith('\\')
                ? Task.FromResult(1)
                : base.OpenWindowCallback(input, caret);
        }

        private async Task OpenHandlerScreen(string[] args)
        {
            using var screen = (HandlerScreen)_screenService.GetScreen(ScreenService.ScreenType.Handlers);
            await screen.Show();
        }

        private async Task OpenHostedFilesScreen(string[] args)
        {
            using var screen = (HostedFilesScreen)_screenService.GetScreen(ScreenService.ScreenType.HostedFiles);
            await screen.Show();
        }
        
        private async Task OpenCredScreen(string[] args)
        {
            var screen = (CredentialScreen)_screenService.GetScreen(ScreenService.ScreenType.Credentials);
            await screen.Show();
        }

        private async Task ListDrones(string[] args)
        {
            var drones = (await _apiService.GetDrones()).ToList();

            foreach (var drone in drones)
            {
                // if it already exists, update it
                var existing = _drones.FirstOrDefault(d => d.Guid.Equals(drone.Guid));
                if (existing is not null)
                {
                    // only need to update these ones, nothing else should change
                    existing.Modules = drone.Modules;
                    existing.LastSeen = drone.LastSeen;
                }
                else
                {
                    _drones.Add(drone);
                }
            }

            var tmpList = new ResultList<Drone>();
            
            var visibleDrones = _drones.Where(d => !d.Hidden);
            tmpList.AddRange(visibleDrones);

            Console.PrintOutput(!tmpList.Any() ? "No Drones" : tmpList.ToString());
        }

        private Task HideDrone(string[] args)
        {
            var guid = args[1];
            
            var drone = _drones.FirstOrDefault(d => d.Guid.Equals(guid));
            if (drone is null)
            {
                Console.PrintError("Unknown Drone.");
                return Task.CompletedTask;
            }
            
            drone.Hide();
            return Task.CompletedTask;
        }

        private async Task GeneratePayload(string[] args)
        {
            var handler = args[1];
            var format = args[2];
            var path = args[3];

            var payload = await _apiService.GeneratePayload(format, handler);
            await File.WriteAllBytesAsync(path, payload.Bytes);
            
            Console.PrintSuccess($"{payload.Bytes.Length} bytes saved.");
        }

        private async Task InteractWithDrone(string[] args)
        {
            var droneGuid = args[1];

            if (!_drones.Any(d => d.Guid.Equals(droneGuid)))
            {
                Console.PrintError("Unknown Drone");
                return;
            }

            using var handlerScreen =
                (DroneInteraction)_screenService.GetScreen(ScreenService.ScreenType.DroneInteract);
            await handlerScreen.SetScreenName(droneGuid);
            await handlerScreen.Show();
        }
        
        private void OnDroneCheckedIn(DroneMetadata metadata)
        {
            // if drone is already in the list
            if (_drones.Any(d => d.Guid.Equals(metadata.Guid)))
            {
                var drone = _drones.Single(d => d.Guid.Equals(metadata.Guid));
                
                // if it's hidden, update properties and show it again
                if (drone.Hidden) drone.Show();
            }
            // else add it
            else
            {
                _drones.Add(new Drone(metadata));
                Console.PrintSuccess($"Drone {metadata.Guid} checked in from {metadata.Username}@{metadata.Hostname}.");
            }
        }

        public void Dispose()
        {
            _signalRService.DroneCheckedIn -= OnDroneCheckedIn;
        }
    }
}