using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TeamServer.Handlers;
using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Services
{
    public class PayloadService : IPayloadService
    {
        private readonly IServerService _server;
        private readonly ICryptoService _crypto;

        public PayloadService(IServerService server, ICryptoService crypto)
        {
            _server = server;
            _crypto = crypto;
        }

        public IEnumerable<string> GetFormats()
        {
            return Enum.GetNames(typeof(PayloadFormat));
        }

        public async Task<Payload> GeneratePayload(PayloadFormat format, Handler handler)
        {
            var profile = _server.GetC2Profile();
            var key = _crypto.GetEncodedKey();

            Payload payload = format switch
            {
                PayloadFormat.Exe => new ExePayload(handler, profile, key),
                PayloadFormat.Dll => new DllPayload(handler, profile, key),
                PayloadFormat.Raw => new RawPayload(handler, profile, key),
                PayloadFormat.Svc => new ServicePayload(handler, profile, key),
                PayloadFormat.PowerShell => new PoshPayload(handler, profile, key),
                
                _ => throw new ArgumentOutOfRangeException()
            };

            await payload.Generate();
            return payload;
        }

        public enum PayloadFormat
        {
            Exe,
            Dll,
            Raw,
            Svc,
            PowerShell
        }
    }
}