using System;
using System.Linq;
using System.Net;

namespace MOUSE.Core
{
    static public class EndpointHelpers
    {
        static public IPEndPoint Parse(string endpointstring)
        {
            if (string.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            string[] values = endpointstring.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                if (values.Length == 1)
                    //no port is specified, default
                    throw new ArgumentException($"No port specified: '{endpointstring}'");
                else
                    port = getPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = getIPfromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = getPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    throw new ArgumentException($"No port specified: '{endpointstring}'");
                }
            }
            else
            {
                throw new FormatException($"Invalid endpoint ipaddress '{endpointstring}'");
            }

            return new IPEndPoint(ipaddy, port);
        }

        static private int getPort(string p)
        {
            int port;

            if (!int.TryParse(p, out port)
                || port < IPEndPoint.MinPort
                || port > IPEndPoint.MaxPort)
            {
                throw new FormatException($"Invalid end point port '{p}'");
            }

            return port;
        }

        static private IPAddress getIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException($"Host not found: {p}");

            return hosts[0];
        }
    }
}