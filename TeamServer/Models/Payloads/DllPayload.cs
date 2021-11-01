using System.IO;
using System.Threading.Tasks;

using dnlib.DotNet;
using dnlib.DotNet.Writer;
using dnlib.PE;

using TeamServer.Handlers;

namespace TeamServer.Models
{
    public class DllPayload : Payload
    {
        public DllPayload(Handler handler, C2Profile c2Profile, string cryptoKey) : base(handler, c2Profile, cryptoKey)
        { }

        public override async Task Generate()
        {
            var drone = await GetDroneModuleDef();
            AddUnmanagedExport(drone);
            
            var opts = new ModuleWriterOptions(drone)
            {
                PEHeadersOptions = { Machine = Machine.AMD64 },
                Cor20HeaderOptions = { Flags = 0 }
            };
            
            await using var ms = new MemoryStream();
            drone.Write(ms, opts);
            Bytes = ms.ToArray();
        }
        
        private void AddUnmanagedExport(ModuleDef module)
        {
            var program = module.Types.GetType("Program");
            var execute = program?.Methods.GetMethod("Execute");
            if (execute is null) return;

            execute.ExportInfo = string.IsNullOrEmpty(C2Profile.Stage.DllExport)
                ? new MethodExportInfo()
                : new MethodExportInfo(C2Profile.Stage.DllExport);

            execute.IsUnmanagedExport = true;
            
            var type = execute.MethodSig.RetType;
            type = new CModOptSig(module.CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "CallConvStdcall"), type);
            execute.MethodSig.RetType = type;
        }
    }
}