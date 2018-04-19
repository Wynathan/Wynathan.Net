using System;

using Wynathan.Net.Http.Models;

namespace Wynathan.Net.Http.Helpers
{
    internal static class HttpHelper
    {
        /// <summary>
        /// Creates a <see cref="Uri"/> based on a <paramref name="domain"/>, Location 
        /// header's value as <paramref name="location"/>, and <paramref name="port"/> 
        /// received. In case <paramref name="location"/> is provided and is a valid 
        /// <see cref="Uri"/>, disregards both <paramref name="domain"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="domain">
        /// A domain to be used as a <see cref="Uri.Host"/> in case unable to create 
        /// <see cref="Uri"/> from <paramref name="location"/>.
        /// </param>
        /// <param name="location">
        /// Can be either fully qualified <see cref="Uri"/> (absolute), or a relative 
        /// path. In former case, the method will return an instance of <see cref="Uri"/> 
        /// class qualified from this parameter; in latter case, the method will create 
        /// a new <see cref="Uri"/> instance using <paramref name="domain"/> as a 
        /// <see cref="Uri.Host"/>, <paramref name="port"/> as a <see cref="Uri.Scheme"/>, 
        /// and this parameter as a <see cref="Uri.PathAndQuery"/>.
        /// </param>
        /// <param name="port">
        /// A domain to be used as a <see cref="Uri.Scheme"/> in case unable to create 
        /// <see cref="Uri"/> from <paramref name="location"/>.
        /// </param>
        /// <returns></returns>
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

        /// <summary>
        /// Parses <paramref name="uri"/>'s <see cref="Uri.Scheme"/> to a 
        /// respective <see cref="HttpPort"/> value.
        /// </summary>
        /// <param name="uri">
        /// A <see cref="Uri"/> to retrieve <see cref="Uri.Scheme"/> from.
        /// </param>
        /// <returns></returns>
        public static HttpPort GetPort(this Uri uri)
        {
            return (HttpPort)Enum.Parse(typeof(HttpPort), uri.Scheme, true);
        }
    }
}
