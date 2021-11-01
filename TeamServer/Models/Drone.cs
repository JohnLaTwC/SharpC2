using System;
using System.Collections.Generic;
using System.Linq;

namespace TeamServer.Models
{
    public class Drone
    {
        public DroneMetadata Metadata { get; }
        public string Parent { get; set; }
        public List<DroneModule> Modules { get; } = new();
        public DateTime LastSeen { get; private set; }

        private readonly List<DroneTask> _tasks = new();

        public Drone(DroneMetadata metadata)
            => Metadata = metadata;

        public void CheckIn()
            => LastSeen = DateTime.UtcNow;

        public void TaskDrone(DroneTask task)
            => _tasks.Add(task);

        public void AddModule(DroneModule module)
            => Modules.Add(module);

        public IEnumerable<DroneTask> GetPendingTasks()
        {
            var tasks = _tasks.Where(t =>
                t.Status == DroneTask.TaskStatus.Pending).ToList();

            if (tasks.Count == 0) return Array.Empty<DroneTask>();

            var copy = new DroneTask[tasks.Count]; 
            tasks.CopyTo(copy, 0);
            tasks.ForEach(t => t.UpdateStatus(DroneTask.TaskStatus.Tasked));            

            return copy;
        }

        public IEnumerable<DroneTask> GetTasks()
            => _tasks.ToArray();

        public DroneTask GetTask(string guid)
            => GetTasks().FirstOrDefault(t => t.TaskGuid.Equals(guid));

        public void DeletePendingTask(DroneTask task)
            => _tasks.Remove(task);
    }
}