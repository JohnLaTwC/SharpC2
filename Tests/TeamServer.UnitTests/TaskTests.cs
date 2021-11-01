using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TeamServer.Interfaces;
using TeamServer.Models;

using Xunit;

namespace TeamServer.UnitTests
{
    public class TaskTests
    {
        private readonly IDroneService _drones;
        private readonly ITaskService _tasks;
        private readonly ICryptoService _crypto;

        public TaskTests(ITaskService tasks, IDroneService drones, ICryptoService crypto)
        {
            _tasks = tasks;
            _drones = drones;
            _crypto = crypto;
        }

        private static IEnumerable<DroneTask> GetTasksFromMessage(C2Message message)
        {
            var json = Encoding.UTF8.GetString(message.Data);
            return json.Deserialize<DroneTask[]>();
        }

        [Fact]
        public async Task GetSingleTaskSingleDrone()
        {
            var metadata = new DroneMetadata { Guid = "1" };
            var drone = new Drone(metadata);

            var task = new DroneTask("TestModule", "TestCommand");
            drone.TaskDrone(task);
            
            _drones.AddDrone(drone);

            var envelopes = (await _tasks.GetDroneTasks(metadata)).ToArray();

            Assert.NotEmpty(envelopes);
            Assert.True(envelopes.Length == 1);
            
            var message = _crypto.DecryptEnvelope(envelopes[0]);
            var tasks = GetTasksFromMessage(message).ToArray();

            Assert.NotNull(message);
            Assert.True(tasks.Any());
            Assert.True(tasks.Length == 1);
            Assert.Equal(tasks[0].Module, task.Module);
            Assert.Equal(tasks[0].Command, task.Command);
        }

        [Fact]
        public async Task GetSingleTaskSingleChildDrone()
        {
            var parentMetadata = new DroneMetadata { Guid = "1" };
            var parentDrone = new Drone(parentMetadata);
            
            var childDrone = new Drone(new DroneMetadata { Guid = "2" })
            {
                Parent = parentMetadata.Guid
            };
            
            var task = new DroneTask("TestModule", "TestCommand");
            childDrone.TaskDrone(task);
            
            _drones.AddDrone(parentDrone);
            _drones.AddDrone(childDrone);

            var envelopes = (await _tasks.GetDroneTasks(parentMetadata)).ToArray();
            
            Assert.NotEmpty(envelopes);
            Assert.True(envelopes.Length == 1);
            
            var message = _crypto.DecryptEnvelope(envelopes[0]);
            var tasks = GetTasksFromMessage(message).ToArray();

            Assert.NotNull(message);
            Assert.True(tasks.Any());
            Assert.True(tasks.Length == 1);
            Assert.Equal(tasks[0].Module, task.Module);
            Assert.Equal(tasks[0].Command, task.Command);
        }

        [Fact]
        public async Task GetTwoTasksTwoChildrenSameParent()
        {
            var metadata = new DroneMetadata { Guid = "1" };
            var parent = new Drone(metadata);
            
            var childOne = new Drone(new DroneMetadata { Guid = "2" })
            {
                Parent = metadata.Guid
            };
            
            var childTwo = new Drone(new DroneMetadata { Guid = "3" })
            {
                Parent = metadata.Guid
            };

            var childOneTask = new DroneTask("TestModule", "TestCommand");
            var childTwoTask = new DroneTask("TestModule", "TestCommand");
            
            childOne.TaskDrone(childOneTask);
            childTwo.TaskDrone(childTwoTask);
            
            _drones.AddDrone(parent);
            _drones.AddDrone(childOne);
            _drones.AddDrone(childTwo);
            
            var envelopes = (await _tasks.GetDroneTasks(metadata)).ToArray();
            
            Assert.NotEmpty(envelopes);
            Assert.True(envelopes.Length == 2);
            
            var childOneMessage = _crypto.DecryptEnvelope(envelopes[0]);
            var childOneTasks = GetTasksFromMessage(childOneMessage).ToArray();
            
            Assert.NotNull(childOneMessage);
            Assert.True(childOneTasks.Any());
            Assert.True(childOneTasks.Length == 1);
            Assert.Equal(childOneTasks[0].Module, childOneTask.Module);
            Assert.Equal(childOneTasks[0].Command, childOneTask.Command);
            
            var childTwoMessage = _crypto.DecryptEnvelope(envelopes[0]);
            var childTwoTasks = GetTasksFromMessage(childTwoMessage).ToArray();

            Assert.NotNull(childTwoMessage);
            Assert.True(childOneTasks.Any());
            Assert.True(childTwoTasks.Length == 1);
            Assert.Equal(childTwoTasks[0].Module, childTwoTask.Module);
            Assert.Equal(childTwoTasks[0].Command, childTwoTask.Command);
        }

        [Fact]
        public async Task GetSingleTaskTwoChildDrones()
        {
            var metadata = new DroneMetadata { Guid = "1" };
            var parent = new Drone(metadata);
            
            var childOne = new Drone(new DroneMetadata { Guid = "2" })
            {
                Parent = metadata.Guid
            };
            
            var childTwo = new Drone(new DroneMetadata { Guid = "3" })
            {
                Parent = childOne.Metadata.Guid
            };
            
            var childTwoTask = new DroneTask("TestModule", "TestCommand");
            childTwo.TaskDrone(childTwoTask);
            
            _drones.AddDrone(parent);
            _drones.AddDrone(childOne);
            _drones.AddDrone(childTwo);

            var envelopes = (await _tasks.GetDroneTasks(metadata)).ToArray();
            
            Assert.NotEmpty(envelopes);
            Assert.True(envelopes.Length == 1);
            
            var message = _crypto.DecryptEnvelope(envelopes[0]);
            var tasks = GetTasksFromMessage(message).ToArray();

            Assert.NotNull(message);
            Assert.True(tasks.Any());
            Assert.True(tasks.Length == 1);
            Assert.Equal(tasks[0].Module, childTwoTask.Module);
            Assert.Equal(tasks[0].Command, childTwoTask.Command);
        }

        [Fact]
        public async Task GetMultipleTasksTwoChildDrones()
        {
            var metadata = new DroneMetadata { Guid = "1" };
            var parent = new Drone(metadata);
            
            var childOne = new Drone(new DroneMetadata { Guid = "2" })
            {
                Parent = metadata.Guid
            };
            
            var childTwo = new Drone(new DroneMetadata { Guid = "3" })
            {
                Parent = childOne.Metadata.Guid
            };
            
            var childOneTask = new DroneTask("TestModule", "TestCommand");
            childOne.TaskDrone(childOneTask);
            
            var childTwoTask = new DroneTask("TestModule", "TestCommand");
            childTwo.TaskDrone(childTwoTask);
            
            _drones.AddDrone(parent);
            _drones.AddDrone(childOne);
            _drones.AddDrone(childTwo);

            var envelopes = (await _tasks.GetDroneTasks(metadata)).ToArray();
            
            Assert.NotEmpty(envelopes);
            Assert.True(envelopes.Length == 2);
            
            var childOneMessage = _crypto.DecryptEnvelope(envelopes[0]);
            var childOneTasks = GetTasksFromMessage(childOneMessage).ToArray();
            
            Assert.NotNull(childOneMessage);
            Assert.True(childOneTasks.Any());
            Assert.True(childOneTasks.Length == 1);
            Assert.Equal(childOneTasks[0].Module, childOneTask.Module);
            Assert.Equal(childOneTasks[0].Command, childOneTask.Command);
            
            var childTwoMessage = _crypto.DecryptEnvelope(envelopes[0]);
            var childTwoTasks = GetTasksFromMessage(childTwoMessage).ToArray();

            Assert.NotNull(childTwoMessage);
            Assert.True(childOneTasks.Any());
            Assert.True(childTwoTasks.Length == 1);
            Assert.Equal(childTwoTasks[0].Module, childTwoTask.Module);
            Assert.Equal(childTwoTasks[0].Command, childTwoTask.Command);
        }
    }
}