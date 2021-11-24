namespace Drone
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            # if DEBUG
            System.Threading.Thread.Sleep(10000);
            #endif
            
            Execute();
        }

        public static void Execute()
        {
            var drone = new Drone();
            drone.Start();
        }
    }
}