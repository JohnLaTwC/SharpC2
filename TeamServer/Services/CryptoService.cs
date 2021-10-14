using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer.Services
{
    public class CryptoService : ICryptoService
    {
        private readonly byte[] _key;

        public CryptoService()
        {
#if DEBUG
        _key = Encoding.UTF8.GetBytes("bmihRwRyhdfwGCa!VJ!97f6tGWPxXD&m");
#else
        _key = GetRandomData(32);
#endif
        }
        
        public MessageEnvelope EncryptMessage(C2Message message)
        {
            var envelope = new MessageEnvelope();
            var raw = message.Serialize();

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = _key;
            aes.GenerateIV();

            using var cryptoTransform = aes.CreateEncryptor();
            envelope.Iv = aes.IV;
            envelope.Data = cryptoTransform.TransformFinalBlock(raw, 0, raw.Length);
            envelope.Hmac = CalculateHmac(envelope.Data);

            return envelope;
        }

        public C2Message DecryptEnvelope(MessageEnvelope envelope)
        {
            if (!ValidHmac(envelope))
                throw new Exception("Invalid HMAC");
            
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Key = _key;
            aes.IV = envelope.Iv;

            using var cryptoTransform = aes.CreateDecryptor();
            var dec = cryptoTransform.TransformFinalBlock(envelope.Data, 0, envelope.Data.Length);

            return dec.Deserialize<C2Message>();
        }

        public string GetEncodedKey()
        {
            return Convert.ToBase64String(_key);
        }

        private byte[] GetRandomData(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var data = new byte[length];
            rng.GetNonZeroBytes(data);
            return data;
        }

        private byte[] CalculateHmac(byte[] data)
        {
            using var hmac = new HMACSHA256(_key);
            return hmac.ComputeHash(data);
        }

        private bool ValidHmac(MessageEnvelope envelope)
        {
            var calculated = CalculateHmac(envelope.Data);
            return calculated.SequenceEqual(envelope.Hmac);
        }
    }
}