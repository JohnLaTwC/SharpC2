using System.Diagnostics;

namespace Drone.Invocation.Injection
{
    public static class Injector
    {
        public static bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, ExecutionTechnique executionTechnique, Process process)
        {
            if (allocationTechnique.IsSupportedPayloadType(payload) == false
                || executionTechnique.IsSupportedPayloadType(payload) == false)
                throw new PayloadTypeNotSupported(payload.GetType());

            return executionTechnique.Inject(payload, allocationTechnique, process);
        }

        public static bool Inject(PayloadType payload, AllocationTechnique allocationTechnique, ExecutionTechnique executionTechnique)
            => Inject(payload, allocationTechnique, executionTechnique, Process.GetCurrentProcess());
    }
}