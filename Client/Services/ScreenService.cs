using System;

using SharpC2.Models;
using SharpC2.Screens;

namespace SharpC2.Services
{
    public class ScreenService
    {
        private readonly ApiService _apiService;
        private readonly SignalRService _signalRService;

        public ScreenService(ApiService apiService, SignalRService signalRService)
        {
            _apiService = apiService;
            _signalRService = signalRService;
        }

        public Screen GetScreen(ScreenType type)
        {
            Screen screen = type switch
            {
                ScreenType.Drones => new DroneScreen(_apiService, _signalRService, this),
                ScreenType.DroneInteract => new DroneInteraction(_apiService, _signalRService),
                ScreenType.Handlers => new HandlerScreen(_apiService, _signalRService),
                ScreenType.HostedFiles => new HostedFilesScreen(_apiService, _signalRService),
                ScreenType.Credentials => new CredentialScreen(_apiService, _signalRService),

                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            return screen;
        }
        
        public enum ScreenType
        {
            Drones,
            DroneInteract,
            Handlers,
            HostedFiles,
            Credentials
        }
    }
}