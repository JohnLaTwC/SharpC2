using System.Collections.Generic;
using System.Threading.Tasks;

using TeamServer.Models;
using TeamServer.Modules;

namespace TeamServer.Interfaces
{
    public interface IServerService
    {
        void SetC2Profile(C2Profile profile);
        C2Profile GetC2Profile();
        Module LoadModule(byte[] bytes);
        Module GetModule(string name);
        IEnumerable<Module> GetModules();
        Task HandleC2Message(MessageEnvelope envelope);
    }
}