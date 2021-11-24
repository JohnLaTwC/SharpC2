using System;

using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Controllers
{
    [ApiController]
    [Authorize]
    [Route(Routes.V1.Credentials)]
    public class CredentialsController : ControllerBase
    {
        private readonly ICredentialService _credentials;
        private readonly IMapper _mapper;

        public CredentialsController(ICredentialService credentials, IMapper mapper)
        {
            _credentials = credentials;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult GetCredentials()
        {
            var credentials = _credentials.GetCredentials();
            return Ok(credentials);
        }

        [HttpGet("{guid}")]
        public IActionResult GetCredential(string guid)
        {
            var credential = _credentials.GetCredential(guid);
            if (credential is null) return NotFound();
            return Ok(credential);
        }

        [HttpPost]
        public IActionResult AddCredentialRecord([FromBody] AddCredentialRecordRequest request)
        {
            var credential = _mapper.Map<AddCredentialRecordRequest, CredentialRecord>(request);
            credential.Guid = Guid.NewGuid().ToShortGuid();
            
            _credentials.AddCredential(credential);

            var response = _mapper.Map<CredentialRecord, CredentialRecordResponse>(credential);
            
            var root = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{HttpContext.Request.Path.ToUriComponent()}";
            var path = $"{root}/{credential.Guid}";

            return Created(path, response);
        }

        [HttpDelete("{guid}")]
        public IActionResult RemoveCredentialRecord(string guid)
        {
            if (_credentials.RemoveCredential(guid))
                return NoContent();
            
            return NotFound();
        }
    }
}