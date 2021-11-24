using System.Collections.Generic;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Controllers
{
    [ApiController]
    [Authorize]
    [Route(Routes.V1.Drones)]
    public class DronesController : ControllerBase
    {
        private readonly IDroneService _drones;
        private readonly IMapper _mapper;
        private readonly IHubContext<MessageHub, IMessageHub> _hub;

        public DronesController(IDroneService drones, IMapper mapper, IHubContext<MessageHub, IMessageHub> hub)
        {
            _drones = drones;
            _mapper = mapper;
            _hub = hub;
        }

        [HttpGet]
        public IActionResult GetDrones()
        {
            var drones = _drones.GetDrones();
            var response = _mapper.Map<IEnumerable<Drone>, IEnumerable<DroneResponse>>(drones);

            return Ok(response);
        }

        [HttpGet("{droneGuid}")]
        public IActionResult GetDrone(string droneGuid)
        {
            var drone = _drones.GetDrone(droneGuid);
            if (drone is null) return NotFound();

            var response = _mapper.Map<Drone, DroneResponse>(drone);
            return Ok(response);
        }

        [HttpPost("{droneGuid}/tasks")]
        public async Task<IActionResult> TaskDrone(string droneGuid, [FromBody] DroneTaskRequest request)
        {
            var drone = _drones.GetDrone(droneGuid);
            if (drone is null) return NotFound();
            
            var task = _mapper.Map<DroneTaskRequest, DroneTask>(request);
            drone.TaskDrone(task);

            var root = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Path.ToUriComponent()}";
            var path = $"{root}/{task.TaskGuid}";

            var response = _mapper.Map<DroneTask, DroneTaskResponse>(task);
            await _hub.Clients.All.DroneTasked(drone.Metadata, response);
            return Created(path, response);
        }

        [HttpGet("{droneGuid}/tasks")]
        public IActionResult GetDroneTasks(string droneGuid)
        {
            var drone = _drones.GetDrone(droneGuid);
            if (drone is null) return NotFound();

            var tasks = drone.GetTasks();
            var response = _mapper.Map<IEnumerable<DroneTask>, IEnumerable<DroneTaskResponse>>(tasks);

            return Ok(response);
        }

        [HttpGet("{droneGuid}/tasks/{taskGuid}")]
        public IActionResult GetDroneTask(string droneGuid, string taskGuid)
        {
            var drone = _drones.GetDrone(droneGuid);
            if (drone is null) return NotFound();

            var task = drone.GetTask(taskGuid);
            if (task is null) return NotFound();

            var response = _mapper.Map<DroneTask, DroneTaskResponse>(task);
            return Ok(response);
        }

        [HttpDelete("{droneGuid}")]
        public async Task<IActionResult> RemoveDrone(string droneGuid)
        {
            var result = _drones.RemoveDrone(droneGuid);
            if (!result) return NotFound();

            await _hub.Clients.All.DroneDeleted(droneGuid);
            return NoContent();
        }

        [HttpDelete("{droneGuid}/tasks/{taskGuid}")]
        public IActionResult DeletePendingTask(string droneGuid, string taskGuid)
        {
            var drone = _drones.GetDrone(droneGuid);
            if (drone is null) return NotFound();

            var task = drone.GetTask(taskGuid);
            if (task is null) return NotFound();

            if (task.Status != DroneTask.TaskStatus.Pending)
                return BadRequest("Task is no longer pending");
            
            drone.DeletePendingTask(task);
            return NoContent();
        }
    }
}