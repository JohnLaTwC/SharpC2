using TeamServer.Models;

namespace TeamServer.Interfaces
{
    public interface ICryptoService
    {
        MessageEnvelope EncryptMessage(C2Message message);
        C2Message DecryptEnvelope(MessageEnvelope envelope);
        string GetEncodedKey();
    }
}