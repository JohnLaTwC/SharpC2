using System.IO;
using System.Threading.Tasks;

using dnlib.DotNet;

using TeamServer.Handlers;

namespace TeamServer.Models
{
    public class ExePayload : Payload
    {
        public ExePayload(Handler handler, C2Profile c2Profile, string cryptoKey) : base(handler, c2Profile, cryptoKey)
        { }
        
        public override async Task Generate()
        {
            var drone = await GetDroneModuleDef();
            ConvertModuleToExe(drone);

            await using var ms = new MemoryStream();
            drone.Write(ms);
            Bytes = ms.ToArray();
        }
        
        private void ConvertModuleToExe(ModuleDef module)
        {
            module.Kind = ModuleKind.Console;

            var program = module.Types.GetType("Program");
            var main = program?.Methods.GetMethod("Main");

            module.EntryPoint = main;
        }
    }
}