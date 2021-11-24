using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SharpC2.Services
{
    public class SslService
    {
        public bool IgnoreSsl { get; set; }
        
        private string _acceptedThumbprint;

        public bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (IgnoreSsl) return true;
            
            var thumbprint = certificate.GetCertHashString();

            if (thumbprint is not null && thumbprint.Equals(_acceptedThumbprint))
                return true;

            Console.WriteLine();
            Console.WriteLine("Server Certificate");
            Console.WriteLine("------------------");
            Console.WriteLine();
            Console.WriteLine(certificate.ToString());

            Console.Write("accept? [y/N] > ");
            
            var accept = string.Empty;

            while (string.IsNullOrEmpty(accept))
                accept = Console.ReadLine();
                
            if (!accept.Equals("Y", StringComparison.OrdinalIgnoreCase)) return false;

            _acceptedThumbprint = thumbprint;
            return true;
        }
    }
}