using System.Collections.Generic;
using System.Threading.Tasks;

using TeamServer.Models;

namespace TeamServer.Interfaces
{
    public interface ITaskService
    {
        Task RecvC2Data(IEnumerable<MessageEnvelope> messages);
        Task<IEnumerable<MessageEnvelope>> GetDroneTasks(DroneMetadata metadata);
    }
}