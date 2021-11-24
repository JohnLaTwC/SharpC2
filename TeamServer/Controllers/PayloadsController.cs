using System;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SharpC2.API.V1;
using SharpC2.API.V1.Responses;

using TeamServer.Interfaces;
using TeamServer.Models;
using TeamServer.Services;

namespace TeamServer.Controllers
{
    [ApiController]
    [Authorize]
    [Route(Routes.V1.Payloads)]
    public class PayloadsController : ControllerBase
    {
        private readonly IPayloadService _payloads;
        private readonly IHandlerService _handlers;
        
        private readonly IMapper _mapper;

        public PayloadsController(IHandlerService handlerService, IPayloadService payloads, IMapper mapper)
        {
            _handlers = handlerService;
            _payloads = payloads;
            
            _mapper = mapper;
        }

        [HttpGet("formats")]
        public IActionResult GetPayloadFormats()
        {
            var formats = _payloads.GetFormats();
            return Ok(formats);
        }
        

        [HttpGet("{handler}/{format}")]
        public async Task<IActionResult> GetPayload(string handler, string format)
        {
            // get the handler
            var h = _handlers.GetHandler(handler);
            if (h is null) return NotFound("Handler not found");

            // parse the format
            if (!Enum.TryParse(format, true, out PayloadService.PayloadFormat payloadFormat))
                return BadRequest("Invalid payload format");

            var payload = await _payloads.GeneratePayload(payloadFormat, h);
            
            var response = _mapper.Map<Payload, PayloadResponse>(payload);
            return Ok(response);
        }
    }
}