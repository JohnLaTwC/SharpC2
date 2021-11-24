using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PrettyPrompt.Completion;

using SharpC2.Models;
using SharpC2.ScreenCommands;
using SharpC2.Services;

namespace SharpC2.Screens
{
    public class DroneInteraction : Screen, IDisposable
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        private Drone _drone;
        
        protected override string ScreenName { get; set; }

        public DroneInteraction(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;
            
            Commands.Add(new BackScreen(StopScreen));

            _signalRService.DroneTasked += OnDroneTasked;
            _signalRService.DroneDataSent += OnDroneDataSent;
            _signalRService.DroneTaskRunning += OnDroneTaskRunning;
            _signalRService.DroneTaskComplete += OnDroneTaskComplete;
            _signalRService.DroneTaskCancelled += OnDroneTaskCancelled;
            _signalRService.DroneTaskAborted += OnDroneTaskAborted;
            _signalRService.DroneModuleLoaded += OnDroneModuleLoaded;
        }

        protected override Task<int> OpenWindowCallback(string input, int caret)
        {
            // if last char is a backslash
            return input.EndsWith('\\')
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
            
            // return arguments based on split length
            var argPosition = splitInput.Length - 2;
            if (argPosition > command.Arguments.Count)
                return base.GetAutoComplete(input, caret);
                
            var argument = command.Arguments[argPosition];
            
            CompletionItem[] result = null;

            if (argument.Artefact)
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

        public override async Task SetScreenName(string name)
        {
            await base.SetScreenName(name);
            _drone = await _apiService.GetDrone(ScreenName);

            foreach (var moduleCommand in _drone.Modules.SelectMany(droneModule => droneModule.Commands))
                Commands.Add(ConvertDroneCommandToScreenCommand(moduleCommand));
        }

        private async Task SendDroneCommand(string[] args)
        {
            // get instance of command, return error if not found
            var command = Commands.FirstOrDefault(c => c.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
            if (command is null)
            {
                Console.PrintError("Unknown command");
                return;
            }
            
            // find the module this command belongs to
            // pretty stupid to have to compare a DroneModule with a ScreenCommand
            // just match the command name and description
            var moduleName = "core";
            
            foreach (var droneModule in _drone.Modules)
            {
                foreach (var moduleCommand in droneModule.Commands)
                {
                    if (!moduleCommand.Name.Equals(command.Name) ||
                        !moduleCommand.Description.Equals(command.Description))
                        continue;

                    moduleName = droneModule.Name;
                    break;
                }
            }

            var submittedArgs = args[1..];

            // grab an artefact if need
            var artefact = Array.Empty<byte>();
            if (command.Arguments.Any(a => a.Artefact))
            {
                // get its index
                var index = command.Arguments.FindIndex(a => a.Artefact);
                var filePath = submittedArgs[index];
                if (!File.Exists(filePath))
                {
                    Console.PrintError("File not found");
                    return;
                }

                artefact = await File.ReadAllBytesAsync(filePath);
                
                // remove the path from the array
                submittedArgs = submittedArgs.Where(a => !a.Equals(filePath)).ToArray();
            }

            await _apiService.SendDroneTask(
                ScreenName,
                moduleName,
                command.Name,
                submittedArgs,
                artefact);
        }

        private ScreenCommand ConvertDroneCommandToScreenCommand(DroneModule.Command command)
        {
            var screenCommandArgs = command.Arguments.Select(commandArgument =>
                new ScreenCommand.Argument
                {
                    Name = commandArgument.Label,
                    Optional = commandArgument.Optional,
                    Artefact = commandArgument.Artefact
                })
                .ToList();

            return new DroneCommand(
                command.Name,
                command.Description,
                screenCommandArgs,
                SendDroneCommand);
        }

        private void OnDroneTasked(DroneMetadata metadata, DroneTask task)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintSuccess($"Tasked Drone to run {task.Command}: {task.TaskGuid}.");
        }
        
        private void OnDroneDataSent(DroneMetadata metadata, int messageSize)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintSuccess($"Drone checked in. Sent {messageSize} bytes.");
        }

        private void OnDroneTaskRunning(DroneMetadata metadata, DroneTaskUpdate update)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintSuccess($"Drone task {update.TaskGuid} is running.");
            
            if (update.Result?.Length > 0)
                Console.PrintOutput(Encoding.UTF8.GetString(update.Result));
        }
        
        private void OnDroneTaskComplete(DroneMetadata metadata, DroneTaskUpdate update)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintSuccess($"Drone task {update.TaskGuid} has completed.");
            
            if (update.Result?.Length > 0)
                Console.PrintOutput(Encoding.UTF8.GetString(update.Result));
        }
        
        private void OnDroneTaskCancelled(DroneMetadata metadata, DroneTaskUpdate update)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintWarning($"Drone task {update.TaskGuid} has been cancelled.");
            
            if (update.Result?.Length > 0)
                Console.PrintOutput(Encoding.UTF8.GetString(update.Result));
        }
        
        private void OnDroneTaskAborted(DroneMetadata metadata, DroneTaskUpdate update)
        {
            if (!metadata.Guid.Equals(_drone.Guid)) return;
            Console.PrintWarning($"Drone task {update.TaskGuid} has thrown an exception.");
            
            if (update.Result?.Length > 0)
                Console.PrintOutput(Encoding.UTF8.GetString(update.Result));
        }

        private void OnDroneModuleLoaded(DroneMetadata metadata, DroneModule module)
        {
            if (!metadata.Guid.Equals(ScreenName)) return;
            
            _drone.Modules.Add(module);
            
            foreach (var moduleCommand in module.Commands)
                Commands.Add(ConvertDroneCommandToScreenCommand(moduleCommand));
            
            Console.PrintSuccess($"Module \"{module.Name}\" loaded with {module.Commands.Count()} commands.");
        }

        public void Dispose()
        {
            _signalRService.DroneTasked -= OnDroneTasked;
            _signalRService.DroneDataSent -= OnDroneDataSent;
            _signalRService.DroneTaskRunning -= OnDroneTaskRunning;
            _signalRService.DroneTaskComplete -= OnDroneTaskComplete;
            _signalRService.DroneTaskCancelled -= OnDroneTaskCancelled;
            _signalRService.DroneTaskAborted -= OnDroneTaskAborted;
            _signalRService.DroneModuleLoaded -= OnDroneModuleLoaded;
        }
    }
}