using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.AspNetCore.SignalR.Client;

using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Services
{
    public class SignalRService
    {
        public string Server { get; set; }
        public string Port { get; set; }
        public string Nick { get; set; }
        public string Password { get; set; }
        
        private readonly SslService _sslService;
        private readonly IMapper _mapper;
        
        public SignalRService(SslService sslService, IMapper mapper)
        {
            _sslService = sslService;
            _mapper = mapper;
        }
        
        // Handler Events
        public event Action<Handler> HandlerStarted;
        public event Action<Handler> HandlerStopped;
        public event Action<string, string> HandlerParameterSet;
        
        // Hosted File Events
        public event Action<string> HostedFileAdded;
        public event Action<string> HostedFileDeleted;
        
        // Drone Events
        public event Action<DroneMetadata> DroneCheckedIn;
        public event Action<DroneMetadata, DroneTask> DroneTasked;
        public event Action<DroneMetadata, int> DroneDataSent;
        public event Action<DroneMetadata, DroneTaskUpdate> DroneTaskRunning;
        public event Action<DroneMetadata, DroneTaskUpdate> DroneTaskComplete;
        public event Action<DroneMetadata, DroneTaskUpdate> DroneTaskCancelled;
        public event Action<DroneMetadata, DroneTaskUpdate> DroneTaskAborted;
        public event Action<DroneMetadata, DroneModule> DroneModuleLoaded;

        public async Task Connect()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"https://{Server}:{Port}/MessageHub", o =>
                {
                    var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Nick}:{Password}"));
                    o.Headers.Add("Authorization", $"Basic {basic}");
                    o.HttpMessageHandlerFactory = _ => new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = _sslService.RemoteCertificateValidationCallback
                    };
                })
                .Build();

            await connection.StartAsync();

            connection.On<string, string>("HandlerParameterSet", OnHandlerParameterSet);
            connection.On<HandlerResponse>("HandlerStarted", OnHandlerStarted);
            connection.On<HandlerResponse>("HandlerStopped", OnHandlerStopped);

            connection.On<string>("HostedFileAdded", OnHostedFileAdded);
            connection.On<string>("HostedFileDeleted", OnHostedFileDeleted);

            connection.On<DroneMetadata>("DroneCheckedIn", OnDroneCheckedIn);
            connection.On<DroneMetadata, DroneTaskResponse>("DroneTasked", OnDroneTasked);
            connection.On<DroneMetadata, int>("DroneDataSent", OnDroneDataSent);
            connection.On<DroneMetadata, DroneTaskUpdate>("DroneTaskRunning", OnDroneTaskRunning);
            connection.On<DroneMetadata, DroneTaskUpdate>("DroneTaskComplete", OnDroneTaskComplete);
            connection.On<DroneMetadata, DroneTaskUpdate>("DroneTaskCancelled", OnDroneTaskCancelled);
            connection.On<DroneMetadata, DroneTaskUpdate>("DroneTaskAborted", OnDroneTaskAborted);
            connection.On<DroneMetadata, DroneModuleResponse>("DroneModuleLoaded", OnDroneModuleLoaded);
        }

        private void OnHandlerStarted(HandlerResponse handlerResponse)
        {
            var handler = _mapper.Map<HandlerResponse, Handler>(handlerResponse);
            HandlerStarted?.Invoke(handler);
        }
        
        private void OnHandlerStopped(HandlerResponse handlerResponse)
        {
            var handler = _mapper.Map<HandlerResponse, Handler>(handlerResponse);
            HandlerStopped?.Invoke(handler);
        }
        
        private void OnHandlerParameterSet(string key, string value)
            => HandlerParameterSet?.Invoke(key, value);

        private void OnHostedFileAdded(string filename)
            => HostedFileAdded?.Invoke(filename);

        private void OnHostedFileDeleted(string filename)
            => HostedFileDeleted?.Invoke(filename);

        private void OnDroneCheckedIn(DroneMetadata metadata)
            => DroneCheckedIn?.Invoke(metadata);

        private void OnDroneTasked(DroneMetadata metadata, DroneTaskResponse taskResponse)
        {
            var task = _mapper.Map<DroneTaskResponse, DroneTask>(taskResponse);
            DroneTasked?.Invoke(metadata, task);
        }
        
        private void OnDroneDataSent(DroneMetadata metadata, int messageSize)
            => DroneDataSent?.Invoke(metadata, messageSize);

        private void OnDroneTaskRunning(DroneMetadata metadata, DroneTaskUpdate update)
            => DroneTaskRunning?.Invoke(metadata, update);
        
        private void OnDroneTaskComplete(DroneMetadata metadata, DroneTaskUpdate update)
            => DroneTaskComplete?.Invoke(metadata, update);
        
        private void OnDroneTaskCancelled(DroneMetadata metadata, DroneTaskUpdate update)
            => DroneTaskCancelled?.Invoke(metadata, update);
        
        private void OnDroneTaskAborted(DroneMetadata metadata, DroneTaskUpdate update)
            => DroneTaskAborted?.Invoke(metadata, update);

        private void OnDroneModuleLoaded(DroneMetadata metadata, DroneModuleResponse response)
        {
            var module = _mapper.Map<DroneModuleResponse, DroneModule>(response);
            DroneModuleLoaded?.Invoke(metadata, module);
        }
    }
}