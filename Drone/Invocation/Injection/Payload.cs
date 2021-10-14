using System;

namespace Drone.Invocation.Injection
{
    public abstract class PayloadType
    {
        public byte[] Payload { get; }
        
        protected PayloadType(byte[] data)
        {
            Payload = data;
        }
    }
    
    public class PICPayload : PayloadType
    {
        public PICPayload(byte[] data) : base(data) { }
    }
    
    public class PayloadTypeNotSupported : Exception
    {
        public PayloadTypeNotSupported() { }
        public PayloadTypeNotSupported(Type payloadType) : base($"Unsupported Payload type: {payloadType.Name}") { }
    }
}