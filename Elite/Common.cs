// Author: Ryan Cobb (@cobbr_io)
// Project: Elite (https://github.com/cobbr/Elite)
// License: GNU GPLv3

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Elite
{
    public static class Common
    {
        public static int EliteMenuUpperBufferLength = 2;
        public static int EliteMenuLowerBufferLength = 2;
        public static string EliteMenuLeftBuffer = "     ";

        public static int GruntMenuGruntNameBufferLength = 13;
        public static int GruntMenuComputerNameBufferLength = 30;
        public static int GruntMenuOperatingSystemBufferLength = 35;

        public static int StagerMenuStagerNameBufferLength = 13;

        public static int ListenerMenuListenerNameBufferLength = 15;
        public static int ListenerMenuIsListeningBufferLength = 14;

        public static int HostedFileMenuListenerNameBufferLength = 11;
        public static int HostedFileMenuHostUriBufferLength = 30;

        public static string EliteRootFolder = Assembly.GetExecutingAssembly().Location.Split("bin")[0].Split("Elite.dll")[0];
        public static string EliteDataFolder = EliteRootFolder + "Data" + Path.DirectorySeparatorChar;
        public static string EliteResourcesFolder = EliteDataFolder + "Resources" + Path.DirectorySeparatorChar;
        public static string EliteDownloadsFolder = EliteDataFolder + "Downloads" + Path.DirectorySeparatorChar;
        public static Encoding CovenantEncoding = Encoding.UTF8;
    }

    public static class Utilities
    {
        public static X509Certificate2 CreateSelfSignedCertificate(string address, string DistinguishedName = "")
        {
            if (DistinguishedName == "") { DistinguishedName = "CN=" + address; }
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(new X500DistinguishedName(DistinguishedName), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                SubjectAlternativeNameBuilder subjectAlternativeName = new SubjectAlternativeNameBuilder();
                subjectAlternativeName.AddIpAddress(IPAddress.Parse(address));

                request.CertificateExtensions.Add(subjectAlternativeName.Build());
                return request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
            }
        }

        public static List<string> GetFilesForPath(string FilePath)
        {
            return Directory.GetFiles(FilePath, "*", SearchOption.AllDirectories)
                            .Select(F => F.Split(FilePath)[1])
                            .Where(F => !F.Contains(".gitignore"))
                            .ToList();
        }

        public static List<string> ParseParameters(string command)
        {
            return Regex.Matches(command, @"[\""].+?[\""]|[^ ]+")
                .Cast<Match>()
                .Select(m => m.Value.Trim('"'))
                .ToList();
        }
    }
}
