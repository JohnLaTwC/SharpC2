using System.Collections.Generic;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using TeamServer.Interfaces;
using TeamServer.Modules;

namespace TeamServer.Controllers
{
    [ApiController]
    [Authorize]
    [Route(Routes.V1.Servers)]
    public class ServerController : ControllerBase
    {
        private readonly IServerService _server;
        private readonly IMapper _mapper;

        public ServerController(IServerService server, IMapper mapper)
        {
            _server = server;
            _mapper = mapper;
        }

        [HttpGet("{modules}")]
        public IActionResult GetModules()
        {
            var modules = _server.GetModules();
            var response = _mapper.Map<IEnumerable<Module>, IEnumerable<ModuleResponse>>(modules);

            return Ok(response);
        }

        [HttpGet("{modules}/{name}")]
        public IActionResult GetModule(string name)
        {
            var module = _server.GetModule(name);

            if (module is null)
                return NotFound();

            var response = _mapper.Map<Module, ModuleResponse>(module);
            return Ok(response);
        }

        [HttpPost("{modules}")]
        public IActionResult LoadServerModule([FromBody] LoadAssemblyRequest request)
        {
            var module = _server.LoadModule(request.Bytes);
            var response = _mapper.Map<Module, ModuleResponse>(module);
            
            var root = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Path.ToUriComponent()}";
            var path = $"{root}/{response.Name}";

            return Created(path, response);
        }
    }
}