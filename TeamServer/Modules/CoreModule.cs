using System.Threading.Tasks;

using TeamServer.Models;

namespace TeamServer.Modules
{
    public class CoreModule : Module
    {
        public override string Name { get; } = "Core";
        public override string Description { get; } = "Handles basic Drone text output.";

        public override async Task Execute(DroneMetadata metadata, DroneTaskUpdate update)
        {
            var drone = Drones.GetDrone(metadata.Guid);
            var task = drone?.GetTask(update.TaskGuid);
            
            task?.UpdateStatus((DroneTask.TaskStatus)update.Status);
            task?.UpdateResult(update.Result);

            // send message to hub
            switch (update.Status)
            {
                case DroneTaskUpdate.TaskStatus.Running:
                    await Hub.Clients.All.DroneTaskRunning(metadata, update);
                    break;
                
                case DroneTaskUpdate.TaskStatus.Complete:
                    await Hub.Clients.All.DroneTaskComplete(metadata, update);
                    break;
                
                case DroneTaskUpdate.TaskStatus.Cancelled:
                    await Hub.Clients.All.DroneTaskCancelled(metadata, update);
                    break;
                
                case DroneTaskUpdate.TaskStatus.Aborted:
                    await Hub.Clients.All.DroneTaskAborted(metadata, update);
                    break;
            }
        }
    }
}