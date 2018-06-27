using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using Wynathan.Net.Http.Helpers;

namespace Wynathan.Net.Http
{
    /// <summary>
    /// Represents an HTTP response model to be used as a result of 
    /// <see cref="HttpRequestClient"/> interactions.
    /// </summary>
    /// <remarks>
    /// TODO: reconsider; replace with a wrapper around request-response 
    /// chain.
    /// </remarks>
    public sealed class HttpResponse
    {
        private string[] plainHttpHeaders;
        private byte[] plainResponseBody;
        private HttpRequest request;

        internal HttpResponse(HttpResponse previous)
        {
            if (previous != null)
            {
                this.Previous = previous;
                this.Previous.Following = this;
            }
        }

        public HttpRequest Request { get; internal set; }

        /// <summary>
        /// The target <see cref="Uri"/> to which original request 
        /// has been made.
        /// </summary>
        public Uri RequestedUri { get; internal set; }

        /// <summary>
        /// The HTTP method that has been used to made the original 
        /// request.
        /// </summary>
        public HttpRequestMethod HttpMethod { get; internal set; }

        /// <summary>
        /// Amount of bytes received in HTTP response body.
        /// </summary>
        public long Bytes { get; internal set; }

        /// <summary>
        /// The <see cref="Uri"/> that returned final status code.
        /// </summary>
        public Uri FinalUri { get; internal set; }

        /// <summary>
        /// The SSL certificate used by the server to establish SSL session.
        /// </summary>
        /// <remarks>
        /// TODO: reconsider; should be a single server cert per 
        /// request made, not for the resulting response wrapper
        /// </remarks>
        public X509Certificate ServerCertificate { get; internal set; }

        /// <summary>
        /// The HTTP status code received when requested the <see cref="FinalUri"/>.
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// Total time spent on receiving data from the server via 
        /// the underlying socket connection.
        /// </summary>
        public TimeSpan Elapsed { get; internal set; }

        /// <summary>
        /// Final response HTTP headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// Final response HTTP body.
        /// </summary>
        public string Body { get; private set; }

        public HttpResponse Previous { get; }

        public HttpResponse Following { get; private set; }

        /// <summary>
        /// Returns reassembled HTTP response with headers and body. 
        /// Disregards Transfer-Encoding if any (header will persist, 
        /// but body will be decoded).
        /// </summary>
        /// <returns></returns>
        public string GetFullResponse()
        {
            // TODO: if "Transfer-Encoding: chunked" - consider vary result on a matter 
            // of chunks (whether to include "chunked" format or return escaped result).
            var mergedHeaders = string.Join("\r\n", this.plainHttpHeaders);
            var mergedHtml = string.Join("\r\n\r\n", mergedHeaders, this.Body);
            return mergedHtml;
        }
        
        internal void SetBody(string[] plainHttpHeaders, byte[] plainResponseBody, long bytes)
        {
            this.plainHttpHeaders = plainHttpHeaders;
            this.plainResponseBody = plainResponseBody;
            this.Headers = HttpHeadersHelper.ParseHeaders(plainHttpHeaders);
            this.Body = HttpBodyHelper.GetBody(this.Headers, plainResponseBody, ref bytes);
            this.Bytes = bytes;
        }
    }
}
