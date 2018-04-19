using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Wynathan.Net.Extensions;
using Wynathan.Net.Http.Helpers;
using Wynathan.Net.Http.Models;

namespace Wynathan.Net.Http
{
    public sealed class HttpRequestClient : IDisposable
    {
        private const int bufferSize = 4096;

        private const string DefaultHeaderConnection = "Connection: keep-alive";
        private const string DefaultHeaderAccept = "Accept: */*";
        private const string DefaultHeaderAcceptEncoding = "Accept-Encoding: gzip, deflate";
        private const string DefaultHeaderUserAgent = "User-Agent: Wynathan's HttpRequestClient <glyebov.vo@gmail.com>";
        private HttpRequest settings;
        private X509Certificate2Collection clientCertificateCollection;

        private readonly Stopwatch loadTimeStopwatch;

        private Uri uri;
        private HttpPort port;

        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private Stream socketStream;
        private byte[] request;

        private string[] headersPlain;
        private long bytes;
        private HttpStatusCode statusCode;
        private byte[] body;

        private List<byte> headersResult;
        private List<byte> bodyResult;
        private byte[] buffer;

        private bool continueReading;
        private int read;
        private bool headerDone;
        private int contentLength;
        private bool requireContentLengthCheck;
        private bool requireChunkedBodyCheck;
        private bool contentLengthCheck;
        private bool chunkedBodyCheck;

        private bool changeMethodToGet;
        private bool shouldContinue;
        private int currentRedirectAmount;

        private HttpResponse response;

        private X509Certificate serverCertificate;
        private readonly List<HttpResponse> requestChainHistory;

        public HttpRequestClient()
        {
            this.loadTimeStopwatch = new Stopwatch();
            this.requestChainHistory = new List<HttpResponse>();
        }

        #region Sync API
        public HttpResponse Execute(HttpRequest request)
        {
            this.Initialize(request);

            do
            {
                this.Connect();
                this.AssembleRequest();
                this.EnableSslIfNecessary();
                this.SendRequest();
                this.ResetReadDependencies();

                while (this.continueReading)
                {
                    this.ContinueTrackingRequestTime();
                    this.ReadResponseChunk();
                    this.PauseTrackingRequestTime();
                    this.ProcessReadData();
                }

                this.PostReadFlush();
                this.FinishReadingData();
                this.PostReadProcessing();
                this.CreateHistoryEntry();
            } while (this.shouldContinue);

            this.CleanupSession();
            return this.CreateResultFromHistory();
        }

        private void Connect()
        {
            if (this.tcpClient == null)
            {
                this.tcpClient = new TcpClient();
                this.tcpClient.Client.ReceiveTimeout = this.settings.SocketReadTimeout;
                this.tcpClient.Connect(this.uri.Host, (int)this.port);

                this.networkStream = this.tcpClient.GetStream();
                this.networkStream.ReadTimeout = this.settings.SocketReadTimeout;
            }
        }

        private void SendRequest()
        {
            // Send the request.
            this.socketStream.Write(this.request, 0, this.request.Length);
            this.socketStream.Flush();
        }

        private void ReadResponseChunk()
        {
            this.read = this.socketStream.Read(this.buffer, 0, bufferSize);
        }

        private void PostReadFlush()
        {
            this.socketStream.Flush();
        }
        #endregion

        #region Async API
        public async Task<HttpResponse> ExecuteAsync(HttpRequest request)
        {
            this.Initialize(request);

            do
            {
                await this.ConnectAsync();
                this.AssembleRequest();
                this.EnableSslIfNecessary();
                await this.SendRequestAsync();
                this.ResetReadDependencies();
                
                while (this.continueReading)
                {
                    this.ContinueTrackingRequestTime();
                    await this.ReadResponseChunkAsync();
                    this.PauseTrackingRequestTime();
                    this.ProcessReadData();
                }

                await this.PostReadFlushAsync();
                this.FinishReadingData();
                this.PostReadProcessing();
                this.CreateHistoryEntry();
            } while (this.shouldContinue);

            this.CleanupSession();
            return this.CreateResultFromHistory();
        }

        private async Task ConnectAsync()
        {
            if (this.tcpClient == null)
            {
                this.tcpClient = new TcpClient();
                this.tcpClient.Client.ReceiveTimeout = this.settings.SocketReadTimeout;
                await this.tcpClient.ConnectAsync(this.uri.Host, (int)this.port);

                this.networkStream = this.tcpClient.GetStream();
                this.networkStream.ReadTimeout = this.settings.SocketReadTimeout;
            }
        }

        private async Task SendRequestAsync()
        {
            // Send the request.
            await this.socketStream.WriteAsync(this.request, 0, this.request.Length);
            await this.socketStream.FlushAsync();
        }

        private async Task ReadResponseChunkAsync()
        {
            this.read = await this.socketStream.ReadAsync(this.buffer, 0, bufferSize);
        }

        private async Task PostReadFlushAsync()
        {
            await this.socketStream.FlushAsync();
        }
        #endregion

        private void Initialize(HttpRequest request)
        {
            this.settings = request;
            if (request.ClientCertificate != null)
                this.clientCertificateCollection = new X509Certificate2Collection(request.ClientCertificate);
            this.InitializeUri(this.settings.Uri);
        }

        private void InitializeUri(Uri uri)
        {
            this.uri = uri;
            this.port = uri.GetPort();
        }

        private void AssembleRequest()
        {
            if (this.changeMethodToGet)
            {
                this.settings.HttpMethod = HttpRequestMethod.Get;
                this.changeMethodToGet = false;
            }

            this.response = new HttpResponse
            {
                RequestedUri = this.uri,
                HttpMethod = this.settings.HttpMethod,
            };

            var headerBuilder = new StringBuilder();

            headerBuilder.AppendLine($"{this.settings.HttpMethod.ToString().ToUpperInvariant()} {this.uri.PathAndQuery} HTTP/1.1");
            headerBuilder.AppendLine($"Host: {this.uri.Host}");

            bool sendBody = true;
            switch (this.settings.HttpMethod)
            {
                case HttpRequestMethod.Get:
                case HttpRequestMethod.Head:
                    sendBody = false;
                    if (!this.settings.HasHeader("Connection"))
                        headerBuilder.AppendLine(DefaultHeaderConnection);
                    break;
                case HttpRequestMethod.Post:
                    headerBuilder.AppendLine($"Content-Length: {this.settings.BodyBytes?.Length ?? 0}");
                    break;
            }

            foreach (var item in this.settings.GetAdditionalHeaderLines())
                headerBuilder.AppendLine(item);

            if (!this.settings.HasHeader("Accept"))
                headerBuilder.AppendLine(DefaultHeaderAccept);

            if (!this.settings.HasHeader("Accept-Encoding"))
                headerBuilder.AppendLine(DefaultHeaderAcceptEncoding);

            if (!this.settings.HasHeader("User-Agent"))
                headerBuilder.AppendLine(DefaultHeaderUserAgent);

            headerBuilder.AppendLine();

            var headerString = headerBuilder.ToString();
            var header = Encoding.UTF8.GetBytes(headerString);
            var body = sendBody 
                ? this.settings.BodyBytes ?? new byte[0] 
                : new byte[0];
            var request = new byte[header.Length + body.Length];

            // Copy headers. Copy body only if one is provided.
            header.CopyTo(request, 0);
            if (sendBody && body.Length > 0)
                body.CopyTo(request, header.Length);

            // Copy to a new array to avoid sending actual request.
            this.settings.RequestCopyWriter?.Invoke(request.ToArray());

            this.request = request;
        }

        private void EnableSslIfNecessary()
        {
            switch (this.port)
            {
                case HttpPort.HTTP:
                    this.socketStream = this.networkStream;
                    break;
                case HttpPort.HTTPS:
                    // Do not ignore server-side certificate validation.
                    // Provide authentication using client-side certificate if one 
                    // is provided. If validating that certificate will fail at the 
                    // server side - let the exception propagate.
                    SslStream ssl;
                    if (this.clientCertificateCollection == null)
                    {
                        ssl = new SslStream(this.networkStream, true, null);
                        ssl.AuthenticateAsClient(this.uri.Host);
                    }
                    else
                    {
                        ssl = new SslStream(this.networkStream, true);
                        ssl.AuthenticateAsClient(this.uri.Host, this.clientCertificateCollection, this.settings.SslProtocols, true);
                    }

                    this.serverCertificate = ssl.RemoteCertificate;
                    this.socketStream = ssl;
                    break;
                default:
                    throw new ArgumentException("Invalid port value");
            }
        }

        private void ResetReadDependencies()
        {
            // Reset the values.
            this.headersPlain = null;
            this.statusCode = (HttpStatusCode)(-1);
            this.bytes = 0;

            // TODO: allocate in ctor, clean up here
            this.headersResult = new List<byte>(256);
            this.bodyResult = new List<byte>(bufferSize);
            this.buffer = new byte[bufferSize];
            this.body = null;

            this.continueReading = true;
            this.read = -1;

            this.loadTimeStopwatch.Reset();
            this.headerDone = false;
            this.contentLength = 0;
            this.requireContentLengthCheck = false;
            this.requireChunkedBodyCheck = false;
            this.contentLengthCheck = false;
            this.chunkedBodyCheck = false;

            this.shouldContinue = false;
            this.currentRedirectAmount = 0;
        }

        private void ContinueTrackingRequestTime()
        {
            this.loadTimeStopwatch.Start();
        }

        private void PauseTrackingRequestTime()
        {
            this.loadTimeStopwatch.Stop();
        }

        private void ProcessReadData()
        {
            // Packets have unclear sizes. Read by 'size' per turn but make sure to not to add nulls.
            var toAdd = this.buffer;
            if (this.read != this.buffer.Length)
                toAdd = this.buffer.Take(this.read).ToArray();

            if (this.headerDone)
            {
                this.bodyResult.AddRange(toAdd);
            }
            else
            {
                int indexAtFullHeader;

                // Copy current header + current part to single array to detect header's end.
                // TODO: perhaps it may make sense if we'd hold all iterations in a separate 
                // var (header won't take too much memory space; perhaps ~1kb). Still, 
                // these copyings are not such a waste.
                var temp = new byte[this.headersResult.Count + toAdd.Length];
                this.headersResult.CopyTo(0, temp, 0, this.headersResult.Count);
                toAdd.CopyTo(temp, this.headersResult.Count);

                // Search for CRLF at current header + current part to detect header's end.
                if (HttpHeadersHelper.ContainsDoubleCrLf(temp, out indexAtFullHeader))
                {
                    this.headerDone = true;
                    int indexAtCurrentPart = indexAtFullHeader - this.headersResult.Count;
                    this.headersResult.AddRange(toAdd.Take(indexAtCurrentPart));
                    this.bodyResult.AddRange(toAdd.Skip(indexAtCurrentPart));

                    this.headersPlain = HttpHeadersHelper.GetHeaders(this.headersResult);
                    this.statusCode = HttpHeadersHelper.GetStatusCode(this.headersPlain);

                    // RFC 2616 Section 4.4. Message Length
                    if (!HttpHeadersHelper.TryRetrieveHeaderValue(this.headersPlain, HttpRequestHeader.ContentLength, out this.contentLength))
                    {
                        string transferEncoding;
                        if (HttpHeadersHelper.TryRetrieveHeaderValue(this.headersPlain, HttpRequestHeader.TransferEncoding, out transferEncoding))
                        {
                            if (transferEncoding.EqualsII("chunked"))
                                this.requireChunkedBodyCheck = true;
                            else
                                throw new NotImplementedException("Transfer-Encoding header value is not chunked.");
                        }
                    }

                    if (this.contentLength > 0)
                    {
                        this.requireContentLengthCheck = true;
                        this.bytes = this.contentLength;
                    }
                }
                else
                {
                    this.headersResult.AddRange(toAdd);
                }
            }

            this.contentLengthCheck = !this.requireContentLengthCheck || this.contentLength == this.bodyResult.Count;
            this.chunkedBodyCheck = !this.requireChunkedBodyCheck || HttpBodyHelper.IsValidChunkedBody(this.bodyResult);

            if (!this.networkStream.DataAvailable && this.headerDone && this.contentLengthCheck && this.chunkedBodyCheck)
                this.continueReading = false;
        }

        private void FinishReadingData()
        {
            // Cleanup SslStream - we will have to use another one either way.
            if (this.port == HttpPort.HTTPS)
                this.socketStream.Dispose();

            if (this.bytes == 0)
                this.bytes = this.bodyResult.Count;

            this.body = this.bodyResult.ToArray();
        }

        private void PostReadProcessing()
        {
            int status = (int)this.statusCode;
            bool isRedirect = status >= 300 && status < 400;
            if (!isRedirect)
            {
                this.CleanupSession();
                return;
            }

            if (this.settings.HttpMethod != HttpRequestMethod.Get && this.settings.HttpMethod != HttpRequestMethod.Head)
            {
                if (!this.settings.AllowRedirectOnPost)
                {
                    this.CleanupSession();
                    return;
                }

                if (this.statusCode == HttpStatusCode.MovedPermanently || this.statusCode == HttpStatusCode.Found)
                {
                    if (this.settings.AllowMethodChangeOnRedirect)
                    {
                        this.CleanupSession();
                        this.changeMethodToGet = true;
                    }
                }
            }

            string location;
            // We expect to get location to redirect to if redirection is presumed by the server.
            // If no Location header found, then to redirection is intended.
            if (!HttpHeadersHelper.TryRetrieveHeaderValue(this.headersPlain, HttpRequestHeader.ContentLocation, out location))
            {
                this.CleanupSession();
                return;
            }

            this.currentRedirectAmount++;
            if (this.currentRedirectAmount >= this.settings.MaximumRedirectAmount)
                throw new InvalidOperationException("Reach redirect threshold.");

            var newUri = HttpHelper.BuildUriFrom(this.uri.Host, location, this.port);

            // If case of statusCode is either 301 or 302, or just hosts differ, 
            // new TCP session is required.
            if (this.uri.Host != newUri.Host)
            {
                var newPort = this.port;
                switch (newUri.Scheme)
                {
                    case "http":
                        newPort = HttpPort.HTTP;
                        break;
                    case "https":
                        newPort = HttpPort.HTTPS;
                        break;
                }

                this.InitializeUri(newUri);
                this.CleanupSession();
                this.shouldContinue = true;
                return;
            }

            var uriPort = newUri.GetPort();
            // In case if ports differ, we should recreate another Tcp session 
            // using another port.
            if (this.port != uriPort)
            {
                this.InitializeUri(newUri);
                this.CleanupSession();
                this.shouldContinue = true;
                return;
            }

            this.InitializeUri(newUri);

            string connection;
            // If the "Connection" header was provided by the server and its value is set "close", 
            // then the socket has already been closed by the server, hence no need to get an 
            // exception in our face - just recreate the connection.
            if (HttpHeadersHelper.TryRetrieveHeaderValue(this.headersPlain, HttpRequestHeader.Connection, out connection) && connection.EqualsII("close"))
            {
                this.CleanupSession();
                this.shouldContinue = true;
                return;
            }

            string keepAlive;
            if (HttpHeadersHelper.TryRetrieveHeaderValue(this.headersPlain, HttpRequestHeader.KeepAlive, out keepAlive))
            {
                // Ignore timeout and recreate TCP session.
                var timeoutSetting = keepAlive.Split(',').Select(x => x.Trim())
                    .FirstOrDefault(x => x.StartsWithII("timeout"));

                if (!string.IsNullOrWhiteSpace(timeoutSetting))
                {
                    this.CleanupSession();
                    this.shouldContinue = true;
                    return;
                }
            }

            // In case we have missed some specific case, just continue using this session.
            this.shouldContinue = true;
        }

        private void CreateHistoryEntry()
        {
            this.response.FinalUri = this.uri;
            this.response.StatusCode = this.statusCode;
            this.response.ServerCertificate = this.serverCertificate;
            this.response.Elapsed = this.loadTimeStopwatch.Elapsed;
            this.response.SetBody(this.headersPlain, this.body, this.bytes);

            this.requestChainHistory.Add(this.response);
        }

        private void CleanupSession()
        {
            this.socketStream?.Dispose();
            this.networkStream?.Dispose();
            this.tcpClient?.Close();

            this.socketStream = null;
            this.networkStream = null;
            this.tcpClient = null;
        }

        private HttpResponse CreateResultFromHistory()
        {
            int amount = this.requestChainHistory.Count;
            var result = this.requestChainHistory[amount - 1];
            for (int i = 0; i < amount - 1; i++)
                result.AddRequestToChain(this.requestChainHistory[i]);
            return result;
        }

        #region IDisposable Support
        private volatile bool disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.CleanupSession();
                }

                this.socketStream = null;
                this.networkStream = null;
                this.tcpClient = null;

                this.disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
