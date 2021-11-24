using CommandLine;

namespace SharpC2
{
    internal class Options
    {
        [Option('s', "server", Required = true, HelpText = "IP or hostname of Team Server.")]
        public string Server { get; set; }
        
        [Option('p', "port", Required = true, HelpText = "Team Server port.")]
        public string Port { get; set; }
        
        [Option('n', "nick", Required = true, HelpText = "Nickname to connect with.")]
        public string Nick { get; set; }
        
        [Option('P', "password", Required = true, HelpText = "Team Server's shared password.")]
        public string Password { get; set; }
        
        [Option('i', "ignore-ssl", Required = false, HelpText = "Ignore SSL warnings.")]
        public bool IgnoreSsl { get; set; }
    }
}