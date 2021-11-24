using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using TeamServer.Handlers;
using TeamServer.Hubs;
using TeamServer.Interfaces;

namespace TeamServer.Controllers
{
    [ApiController]
    [Authorize]
    [Route(Routes.V1.HostedFiles)]
    public class HostedFilesController : ControllerBase
    {
        private readonly IHandlerService _handlers;
        private readonly IHubContext<MessageHub, IMessageHub> _hub;

        public HostedFilesController(IHandlerService handlers, IHubContext<MessageHub, IMessageHub> hub)
        {
            _handlers = handlers;
            _hub = hub;
        }

        [HttpGet]
        public IActionResult GetHostedFiles()
        {
            var handler = (DefaultHttpHandler) _handlers.GetHandler("default-http");
            var fileInfos = handler.GetHostedFiles();
            var response = fileInfos.Select(fileInfo =>
                new HostedFileResponse { Filename = fileInfo.Name, Size = fileInfo.Length }).ToList();

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromBody] AddHostedFileRequest request)
        {
            var handler = (DefaultHttpHandler) _handlers.GetHandler("default-http");

            if (!handler.Running)
            {
                var task = handler.Start();
                if (task.IsFaulted) return BadRequest(task.Exception?.Message);
            }

            await handler.AddHostedFile(request.Content, request.Filename);
            
            var connectAddress = handler.Parameters.Single(p =>
                p.Name.Equals("ConnectAddress", StringComparison.OrdinalIgnoreCase)).Value;
            
            var bindPort = handler.Parameters.Single(p =>
                p.Name.Equals("BindPort", StringComparison.OrdinalIgnoreCase)).Value;

            var path = $"http://{connectAddress}:{bindPort}/{request.Filename}";
            await _hub.Clients.All.HostedFileAdded(request.Filename);
            return Created(path, request.Content);
        }

        [HttpDelete("{filename}")]
        public IActionResult DeleteFile(string filename)
        {
            var handler = (DefaultHttpHandler)_handlers.GetHandler("default-http");
            if (!handler.RemoveHostedFile(filename)) return NotFound();

            _hub.Clients.All.HostedFileDeleted(filename);
            return NoContent();
        }
    }
}