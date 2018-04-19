using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

using Wynathan.Net.Http.Helpers;

namespace Wynathan.Net.Http
{
    public sealed class HttpResponse
    {
        private string[] plainHttpHeaders;
        private byte[] plainResponseBody;
        private readonly List<HttpResponse> requestChain;

        public Uri RequestedUri;
        public HttpRequestMethod HttpMethod;
        public long Bytes;
        public Uri FinalUri;
        public X509Certificate ServerCertificate;
        public HttpStatusCode StatusCode;
        public TimeSpan Elapsed;

        internal HttpResponse()
        {
            this.requestChain = new List<HttpResponse>();
        }
        
        public Dictionary<string, string> Headers { get; private set; }

        public string Body { get; private set; }

        public string GetFullResponse()
        {
            // TODO: if "Transfer-Encoding: chunked" - consider vary result on a matter 
            // of chunks (whether to include "chunked" format or return escaped result).
            var mergedHeaders = string.Join("\r\n", this.plainHttpHeaders);
            var mergedHtml = string.Join("\r\n\r\n", mergedHeaders, this.Body);
            return mergedHtml;
        }

        public IEnumerable<HttpResponse> GetRequestChain()
        {
            return this.requestChain.ToArray();
        }

        internal void SetRequestChain(IEnumerable<HttpResponse> requestChain)
        {
            foreach (var request in requestChain)
                this.requestChain.Add(request);
        }

        internal void AddRequestToChain(HttpResponse request)
        {
            this.requestChain.Add(request);
        }

        internal void SetBody(string[] plainHttpHeaders, byte[] plainResponseBody, long bytes)
        {
            this.plainHttpHeaders = plainHttpHeaders;
            this.plainResponseBody = plainResponseBody;
            this.Headers = HttpHeadersHelper.ParseHeaders(plainHttpHeaders);
            this.Body = HtmlBodyHelper.GetHtml(this.Headers, plainResponseBody, ref bytes);
            this.Bytes = bytes;
        }
    }
}
