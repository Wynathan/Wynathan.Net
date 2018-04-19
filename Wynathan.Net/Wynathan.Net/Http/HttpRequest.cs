using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Wynathan.Net.Extensions;
using Wynathan.Net.Http.Helpers;
using Wynathan.Net.Http.Models;

namespace Wynathan.Net.Http
{
    public sealed class HttpRequest
    {
        private byte[] bodyBytes;
        private string body;

        private readonly Dictionary<string, string> additionalHeaders;
        
        public Uri Uri;
        public SslProtocols SslProtocols;
        public int MaximumRedirectAmount;
        public int SocketReadTimeout;
        public HttpRequestMethod HttpMethod;
        public X509Certificate2 ClientCertificate;
        public Action<byte[]> RequestCopyWriter;

        public bool AllowRedirectOnPost;
        public bool AllowMethodChangeOnRedirect;

        public HttpRequest()
        {
            this.AllowRedirectOnPost = false;
            this.AllowMethodChangeOnRedirect = false;

            this.additionalHeaders = new Dictionary<string, string>();
            this.SslProtocols = SslProtocols.Tls12 | SslProtocols.Ssl3;
            this.MaximumRedirectAmount = 30;
            this.SocketReadTimeout = 5000;
            this.HttpMethod = HttpRequestMethod.Get;
        }

        public HttpRequest(string domain) : this()
        {
            this.Domain = domain;
        }

        public HttpRequest(Uri uri) : this()
        {
            this.Uri = uri;
        }

        public string Domain
        {
            get
            {
                return this.Uri.Host;
            }
            set
            {
                this.Uri = HttpHelper.BuildUriFrom(value, null, HttpPort.HTTP);
            }
        }
        
        public string Body
        {
            get
            {
                return this.body;
            }
            set
            {
                this.body = value;
                this.bodyBytes = Encoding.UTF8.GetBytes(value);
                this.HttpMethod = HttpRequestMethod.Post;
            }
        }

        public byte[] BodyBytes
        {
            get
            {
                return this.bodyBytes;
            }
            set
            {
                this.bodyBytes = value;
                this.body = Encoding.UTF8.GetString(value);
                this.HttpMethod = HttpRequestMethod.Post;
            }
        }

        public void AddHeader(string key, string value)
        {
            this.additionalHeaders[key.Trim()] = value.Trim();
        }

        public IEnumerable<string> GetAdditionalHeaders()
        {
            return this.additionalHeaders.Select(x => string.Join(": ", x.Key, x.Value));
        }

        internal bool HasHeader(string key)
        {
            var trimmedKey = key.Trim();
            return this.additionalHeaders.Keys.Any(x => x.EqualsII(trimmedKey));
        }
    }
}
