namespace TeamServer.Models
{
    public class C2Profile
    {
        public StageBlock Stage { get; set; } = new();
        public PostExploitationBlock PostExploitation { get; set; } = new();
        public ProcessInjectionBlock ProcessInjection { get; set; } = new();

        public class StageBlock
        {
            public string SleepTime { get; set; } = "60";
            public string SleepJitter { get; set; } = "10";
            public string DllExport { get; set; } = "Execute";
        }

        public class PostExploitationBlock
        {
            public bool BypassAmsi { get; set; } = false;
            public bool BypassEtw { get; set; } = false;
            public string SpawnTo { get; set; } = @"C:\Windows\System32\notepad.exe";
            public string AppDomain { get; set; } = "SharpC2";
        }

        public class ProcessInjectionBlock
        {
            public string Allocation { get; set; } = "NtWriteVirtualMemory";
            public string Execution { get; set; } = "RtlCreateUserThread";
        }
    }
}