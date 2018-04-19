using System;

using Wynathan.Net.Http.Models;

namespace Wynathan.Net.Http.Helpers
{
    internal static class HttpHelper
    {
        public static Uri BuildUriFrom(string domain, string location, HttpPort port)
        {
            var actualLocation = location;
            Uri uri;
            if (!Uri.TryCreate(actualLocation, UriKind.Absolute, out uri))
            {
                var uriBuilder = new UriBuilder(port.ToString(), domain);
                uriBuilder.Path = location;
                actualLocation = uriBuilder.ToString();
            }
            return new Uri(actualLocation);
        }

        public static HttpPort GetPort(this Uri uri)
        {
            return (HttpPort)Enum.Parse(typeof(HttpPort), uri.Scheme, true);
        }
    }
}
