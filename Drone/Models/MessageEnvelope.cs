using System;
using System.Runtime.Serialization;

namespace Drone.Models
{
    [DataContract]
    public class MessageEnvelope
    {
        [DataMember (Name = "drone")]
        public string Drone { get; set; }
        
        [DataMember (Name = "iv")]
        public string Iv { get; set; }

        [DataMember (Name = "data")]
        public string Data { get; set; }
        
        [DataMember (Name = "hmac")]
        public string Hmac { get; set; }

        [IgnoreDataMember]
        public byte[] IvBytes
        {
            get => string.IsNullOrWhiteSpace(Iv) ? Array.Empty<byte>() : Convert.FromBase64String(Iv);
            set => Iv = Convert.ToBase64String(value);
        }

        [IgnoreDataMember]
        public byte[] DataBytes
        {
            get => string.IsNullOrWhiteSpace(Iv) ? Array.Empty<byte>() : Convert.FromBase64String(Data);
            set => Data = Convert.ToBase64String(value);
        }

        [IgnoreDataMember]
        public byte[] HmacBytes
        {
            get => string.IsNullOrWhiteSpace(Iv) ? Array.Empty<byte>() : Convert.FromBase64String(Hmac);
            set => Hmac = Convert.ToBase64String(value);
        }
    }
}