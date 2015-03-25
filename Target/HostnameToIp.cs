using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace Gelf4NLog.Target
{
    public static class HostnameToIp
    {
        private static readonly Regex IpAddressRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");

        public static IPAddress Parse(string addressOrHostname)
        {
            if (IpAddressRegex.IsMatch(addressOrHostname))
            {
                return IPAddress.Parse(addressOrHostname);
            }

            return Dns.GetHostAddresses(addressOrHostname)
                .FirstOrDefault();
        }
    }
}