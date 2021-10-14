using System.Security.Cryptography.X509Certificates;

namespace Drone.Invocation.Utilities
{
    public static class Utilities
    {
        public static bool FileHasValidSignature(string filePath)
        {
            X509Certificate2 fileCertificate;
            
            try
            {
                var signer = X509Certificate.CreateFromSignedFile(filePath);
                fileCertificate = new X509Certificate2(signer);
            }
            catch
            {
                return false;
            }

            var certificateChain = new X509Chain
            {
                ChainPolicy =
                {
                    RevocationFlag = X509RevocationFlag.EntireChain,
                    RevocationMode = X509RevocationMode.Offline,
                    VerificationFlags = X509VerificationFlags.NoFlag
                }
            };

            return certificateChain.Build(fileCertificate);
        }
    }
}