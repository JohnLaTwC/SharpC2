using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using AutoMapper;

using SharpC2.API.V1;
using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Services
{
    public class ApiService
    {
        public string Server { get; set; }
        public string Port { get; set; }
        public string Nick { get; set; }
        public string Password { get; set; }
        public bool IgnoreSsl { get; set; }
        
        private readonly SslService _sslService;
        private readonly IMapper _mapper;
        
        private SharpC2Client _client;
        
        public ApiService(SslService sslService, IMapper mapper)
        {
            _sslService = sslService;
            _mapper = mapper;
        }

        public void StartClient()
        {
            _sslService.IgnoreSsl = IgnoreSsl;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = _sslService.RemoteCertificateValidationCallback
            };

            _client = new SharpC2Client(Server, Port, Nick, Password, handler);
        }

        #region Handlers

        public async Task<IEnumerable<Handler>> GetHandlers()
        {
            var handlers = await _client.GetHandlers();
            return _mapper.Map<IEnumerable<HandlerResponse>, IEnumerable<Handler>>(handlers);
        }

        public async Task<Handler> SetHandlerParameter(string handler, string key, string value)
        {
            var handlerResponse = await _client.SetHandlerParameter(handler, key, value);
            return _mapper.Map<HandlerResponse, Handler>(handlerResponse);
        }

        public async Task StartHandler(string handlerName)
        {
            await _client.StartHandler(handlerName);
        }

        public async Task StopHandler(string handlerName)
        {
            await _client.StopHandler(handlerName);
        }

        #endregion

        #region Drones

        public async Task<IEnumerable<Drone>> GetDrones()
        {
            var drones = await _client.GetDrones();
            return _mapper.Map<IEnumerable<DroneResponse>, IEnumerable<Drone>>(drones);
        }

        public async Task<Drone> GetDrone(string guid)
        {
            var drone = await _client.GetDrone(guid);
            return _mapper.Map<DroneResponse, Drone>(drone);
        }

        public async Task SendDroneTask(string guid, string module, string command, string[] arguments, byte[] artefact)
        {
            var request = new DroneTaskRequest
            {
                Module = module,
                Command = command,
                Arguments = arguments,
                Artefact = artefact
            };

            await _client.SendDroneTask(guid, request);
        }

        #endregion

        #region Payloads

        public async Task<IEnumerable<string>> GetPayloadFormats()
        {
            return await _client.GetPayloadFormats();
        }

        public async Task<Payload> GeneratePayload(string format, string handler)
        {
            var response = await _client.GetPayload(format, handler);
            return _mapper.Map<PayloadResponse, Payload>(response);
        }

        #endregion

        #region HostedFiles

        public async Task<IEnumerable<HostedFile>> GetHostedFiles()
        {
            var files = await _client.GetHostedFiles();
            return _mapper.Map<IEnumerable<HostedFileResponse>, IEnumerable<HostedFile>>(files);
        }

        public async Task AddHostedFile(string filename, byte[] content)
        {
            var request = new AddHostedFileRequest
            {
                Filename = filename,
                Content = content
            };

            await _client.AddHostedFile(request);
        }

        public async Task DeleteHostedFile(string filename)
        {
            await _client.DeleteHostedFile(filename);
        }

        #endregion

        #region Credentials

        public async Task<IEnumerable<CredentialRecord>> GetCredentials()
        {
            var response = await _client.GetCredentials();
            return _mapper.Map<IEnumerable<CredentialRecordResponse>, IEnumerable<CredentialRecord>>(response);
        }

        public async Task<bool> AddCredential(string username, string password, string domain = ".", string source = "manual")
        {
            var request = new AddCredentialRecordRequest
            {
                Username = username,
                Domain = domain,
                Password = password,
                Source = source
            };

            return await _client.AddCredential(request);
        }

        public async Task<bool> DeleteCredential(string guid)
        {
            return await _client.DeleteCredential(guid);
        }

        #endregion
    }
}