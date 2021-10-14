namespace TeamServer.Models
{
    public class MessageEnvelope
    {
        public string Drone { get; set; }
        public byte[] Iv { get; set; }
        public byte[] Data { get; set; }
        public byte[] Hmac { get; set; }
    }
}