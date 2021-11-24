using System.Collections.Generic;
using System.Threading.Tasks;

using TeamServer.Handlers;
using TeamServer.Models;
using TeamServer.Services;

namespace TeamServer.Interfaces
{
    public interface IPayloadService
    {
        IEnumerable<string> GetFormats();
        Task<Payload> GeneratePayload(PayloadService.PayloadFormat format, Handler handler);
    }
}