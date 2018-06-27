using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Wynathan.Net.Extensions;
using Wynathan.Net.Http.Helpers;

namespace Wynathan.Net.Http
{
    /// <summary>
    /// Represents an HTTP request model to be used in conjunction with 
    /// <see cref="HttpRequestClient"/> to operate HTTP interactions.
    /// </summary>
    public sealed class HttpRequest : ICopyable<HttpRequest>
    {
        private byte[] assembledRequest;
        private string assembledRequestString;
        private bool resetAssembledRequestString;

        private byte[] bodyBytes;
        private string body;

        public HttpProtocolVersion HttpVersion;

        /// <summary>
        /// Specifies target <see cref="System.Uri"/>.
        /// </summary>
        public Uri Uri;
        /// <summary>
        /// Specifies possible versions of <see cref="SslProtocols"/> 
        /// to be used for the request.
        /// <para>Defaults to <see cref="SslProtocols.Tls12"/> | <see cref="SslProtocols.Ssl3"/>.</para>
        /// </summary>
        public SslProtocols SslProtocols;
        /// <summary>
        /// Specifies maximum allowed redirects amount during the request. 
        /// When this threshold is reached, an exception should be expected.
        /// <para>Defaults to 30.</para>
        /// </summary>
        public int MaximumRedirectAmount;
        /// <summary>
        /// Specifies maximum allowed timeout for socket read operation in 
        /// milliseconds. When this threshold is reached, an exception should 
        /// be expected.
        /// <para>Defaults to 5000 ms.</para>
        /// </summary>
        public int SocketReadTimeout;
        /// <summary>
        /// Specifies an HTTP method to be sent.
        /// <para>Defaults to <see cref="HttpRequestMethod.Get"/>.</para>
        /// </summary>
        public HttpRequestMethod HttpMethod;
        /// <summary>
        /// Specifies an SSL certificate to be used to authenticate the request 
        /// as a client. If not specified, an attempt to authenticate without 
        /// client SSL certificate will be commenced. Required only for HTTPS 
        /// requests, hence disregarded for HTTP requests.
        /// <para>Defaults to null.</para>
        /// </summary>
        public X509Certificate2 ClientCertificate;
        /// <summary>
        /// Specifies a callback that will receive a copy of a request to be 
        /// sent. Will be called on each consecutive request made until the 
        /// result is received.
        /// <para>Defaults to null.</para>
        /// </summary>
        /// <remarks>
        /// TODO: reconsider its necessity (either remove, or replace with 
        /// an optional logger of some kind);
        /// TODO: reconsider delegate type; perhaps additional meta-data can 
        /// be handy;
        /// </remarks>
        public Action<byte[]> RequestCopyWriter;

        /// <summary>
        /// Specifies whether an underlying <see cref="HttpRequestClient"/> 
        /// should attempt a redirect on an HTTP method other than 
        /// <see cref="HttpRequestMethod.Get"/> or <see cref="HttpRequestMethod.Head"/> 
        /// if redirect response is recieved.
        /// <para>Defaults to false.</para>
        /// </summary>
        public bool AllowRedirectOnPost;
        /// <summary>
        /// Specifies whether an underlying <see cref="HttpRequestClient"/> 
        /// should change requesting method from an HTTP method other than 
        /// <see cref="HttpRequestMethod.Get"/> or <see cref="HttpRequestMethod.Head"/> 
        /// to an <see cref="HttpRequestMethod.Get"/> method in case 
        /// <see cref="AllowRedirectOnPost"/> is set to true.
        /// If <see cref="AllowRedirectOnPost"/> is set to true 
        /// and <see cref="AllowMethodChangeOnRedirect"/> is set to false, 
        /// then an attempt to redirect using the same method as the original 
        /// one will be commenced.
        /// <para>Defaults to false.</para>
        /// </summary>
        public bool AllowMethodChangeOnRedirect;

        public RemoteCertificateValidationCallback ServerCertificateValidationCallback;

        /// <summary>
        /// Creates a new instance of <see cref="HttpRequest"/> type and 
        /// populates it with default values.
        /// </summary>
        public HttpRequest()
        {
            this.HttpVersion = HttpProtocolVersion.Unspecified;
            this.AllowRedirectOnPost = false;
            this.AllowMethodChangeOnRedirect = false;

            this.Headers = new Dictionary<string, string>();
            this.SslProtocols = SslProtocols.Tls12 | SslProtocols.Ssl3;
            this.MaximumRedirectAmount = 30;
            this.SocketReadTimeout = 5000;
            this.HttpMethod = HttpRequestMethod.Get;
        }

        /// <summary>
        /// Creates a new instance of <see cref="HttpRequest"/> type,  
        /// populates it with default values, and sets up <see cref="Uri"/> 
        /// using <paramref name="domain"/> and <see cref="HttpPort.HTTP"/>.
        /// </summary>
        /// <param name="domain">
        /// A host name to populate <see cref="Uri"/> from.
        /// </param>
        public HttpRequest(string domain) 
            : this()
        {
            this.Domain = domain;
        }

        /// <summary>
        /// Creates a new instance of <see cref="HttpRequest"/> type, 
        /// populates it with default values, and populates <see cref="Uri"/> 
        /// from the parameter.
        /// </summary>
        /// <param name="uri">
        /// A <see cref="System.Uri"/> value to populate <see cref="Uri"/> from.
        /// </param>
        public HttpRequest(Uri uri) 
            : this()
        {
            this.Uri = uri;
        }

        /// <summary>
        /// An <see cref="Uri"/>'s host name.
        /// <para>
        /// If setter is used, then <see cref="Uri"/> will be populated 
        /// from the value as a host name, and <see cref="HttpPort.HTTP"/> as 
        /// a scheme identifier.
        /// </para>
        /// </summary>
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
        
        /// <summary>
        /// Specifies the body of the request to be sent. Interchangeable with <see cref="BodyBytes"/>.
        /// <para>
        /// If setter is used, then <see cref="HttpMethod"/> will be set 
        /// to <see cref="HttpRequestMethod.Post"/> automatically.
        /// </para>
        /// </summary>
        public string Body
        {
            get
            {
                return this.body;
            }
            set
            {
                this.body = value;
                if (value == null)
                {
                    this.bodyBytes = null;
                }
                else
                {
                    this.bodyBytes = Encoding.UTF8.GetBytes(value);
                    // TODO: provide check for Put and Update
                    if (this.HttpMethod != HttpRequestMethod.Post && this.HttpMethod != HttpRequestMethod.Delete)
                        this.HttpMethod = HttpRequestMethod.Post;
                }
            }
        }

        /// <summary>
        /// Specifies the body of the request to be sent. Interchangeable with <see cref="Body"/>.
        /// <para>
        /// If setter is used, then <see cref="HttpMethod"/> will be set 
        /// to <see cref="HttpRequestMethod.Post"/> automatically.
        /// </para>
        /// </summary>
        public byte[] BodyBytes
        {
            get
            {
                return this.bodyBytes;
            }
            set
            {
                this.bodyBytes = value;
                if (value == null)
                {
                    this.body = null;
                }
                else
                {
                    this.body = Encoding.UTF8.GetString(value);
                    // TODO: provide check for Put and Update
                    if (this.HttpMethod != HttpRequestMethod.Post && this.HttpMethod != HttpRequestMethod.Delete)
                        this.HttpMethod = HttpRequestMethod.Post;
                }
            }
        }

        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// Adds a header to all consequent underlying requests. In case header 
        /// with the same <paramref name="key"/> has already been added, its 
        /// value will be complemented with the input <paramref name="value"/>.
        /// </summary>
        /// <param name="key">
        /// Header's name.
        /// </param>
        /// <param name="value">
        /// Header's value.
        /// </param>
        public void AddHeader(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            var trimmedKey = key.Trim();
            string existingValue;
            if (this.GetHeader(ref trimmedKey, out existingValue))
                value = string.Join(HttpHeadersHelper.RFC2616MultipleFieldValueHeaderSeparator, existingValue, value);

            this.Headers[trimmedKey] = value.Trim();
        }

        public string GetHttpRequest()
        {
            if (this.resetAssembledRequestString)
            {
                if (this.assembledRequest == null)
                    this.assembledRequestString = null;
                else
                    this.assembledRequestString = Encoding.UTF8.GetString(this.assembledRequest);
                this.resetAssembledRequestString = false;
            }

            return this.assembledRequestString;
        }

        internal void SetAssembledRequest(byte[] request)
        {
            this.resetAssembledRequestString = true;

            if (request.IsNullOrEmpty())
            {
                this.assembledRequest = null;
            }
            else
            {
                this.assembledRequest = new byte[request.Length];
                Array.Copy(request, this.assembledRequest, request.Length);
            }
        }

        internal IEnumerable<string> GetAdditionalHeaderLines()
        {
            return this.Headers.Select(x => string.Join(": ", x.Key, x.Value));
        }

        internal bool HasHeader(string key)
        {
            var trimmedKey = key.Trim();
            return this.Headers.Keys.Any(x => x.EqualsII(trimmedKey));
        }

        internal bool GetHeader(ref string key, out string value)
        {
            value = null;
            var trimmedKey = key.Trim();
            var existingKey = this.Headers.Keys.FirstOrDefault(x => x.EqualsII(trimmedKey));
            if (string.IsNullOrWhiteSpace(existingKey))
                return false;

            key = existingKey;
            value = this.Headers[existingKey];
            return true;
        }

        HttpRequest ICopyable<HttpRequest>.ShallowCopy()
        {
            return (this as ICopyable<HttpRequest>).DeepCopy();
        }

        HttpRequest ICopyable<HttpRequest>.DeepCopy()
        {
            var copy = new HttpRequest(this.Uri)
            {
                AllowMethodChangeOnRedirect = this.AllowMethodChangeOnRedirect,
                AllowRedirectOnPost = this.AllowRedirectOnPost,
                BodyBytes = this.BodyBytes,
                ClientCertificate = this.ClientCertificate,
                Domain = this.Domain,
                HttpMethod = this.HttpMethod,
                HttpVersion = this.HttpVersion,
                MaximumRedirectAmount = this.MaximumRedirectAmount,
                RequestCopyWriter = this.RequestCopyWriter,
                ServerCertificateValidationCallback = this.ServerCertificateValidationCallback,
                SocketReadTimeout = this.SocketReadTimeout,
                SslProtocols = this.SslProtocols
            };

            foreach (var header in this.Headers)
                copy.AddHeader(header.Key, header.Value);

            copy.SetAssembledRequest(this.assembledRequest);

            return copy;
        }
    }
}
