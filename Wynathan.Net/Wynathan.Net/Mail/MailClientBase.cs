using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

using Wynathan.Net.Mail.Helpers;
using Wynathan.Net.Mail.Models;

namespace Wynathan.Net.Mail
{
    public abstract class MailClientBase<THistoryEntry> : IDisposable
        where THistoryEntry : RequestResponseContainer
    {
        // TODO: add a method for generic send/receive approach; 
        // it should contain a check on delayAuth and IsAuthenticated 
        // to auth the client on first call, as well as when the 
        // session timed out

        private const int bufferSize = 4096;

        protected readonly string host;
        protected readonly MailPort port;

        protected readonly Stopwatch loadTimeStopwatch;

        #region TODO: vary by settings
        protected readonly bool delayAuth = false;
        protected readonly int socketReadTimeout = 5000;
        #endregion

        private TcpClient tcpClient;
        private NetworkStream networkStream;

        private readonly byte[] buffer;
        private byte[] currentRead;
        private int currentReadOffset;
        private int read;

        private bool isAuthenticated;
        private string email;
        private string password;
        
        private readonly List<THistoryEntry> history;

        protected MailClientBase(string host, MailPort port)
        {
            this.host = host;
            this.port = port;
            this.loadTimeStopwatch = new Stopwatch();
            this.history = new List<THistoryEntry>();

            this.buffer = new byte[bufferSize];
            this.currentRead = new byte[bufferSize];
        }

        protected bool IsAuthenticated { get { return this.isAuthenticated; } }

        protected string Email { get { return this.email; } }
        
        public bool Authenticate(string email, string password)
        {
            if (this.AreCredentialsNew(email, password))
            {
                this.isAuthenticated = false;
                this.email = email.Trim().ToLowerInvariant();
                this.password = password;

                if (!this.TryEstablishSession())
                {
                    this.TryShutdownSession();
                    if (this.delayAuth)
                        this.TryEstablishSession();
                }

                if (!this.delayAuth)
                    this.SendAuthenticationRequest(this.email, this.password);
            }

            return this.isAuthenticated;
        }

        protected bool TryEstablishSession()
        {
            if (this.tcpClient == null)
            {
                this.tcpClient = new TcpClient();
                this.tcpClient.Client.ReceiveTimeout = socketReadTimeout;
                this.tcpClient.Connect(this.host, (int)this.port);

                this.networkStream = this.tcpClient.GetStream();
                this.networkStream.ReadTimeout = socketReadTimeout;

                this.Send(null);
                return true;
            }

            return false;
        }

        protected bool TryShutdownSession()
        {
            try
            {
                var shutdownMessage = this.CreateSessionShutdownMessage();
                this.Send(shutdownMessage);
                this.CleanupSession();
                return true;
            }
            catch (IOException)
            {
                // The session has already been shutdown by the server.
                // TODO: consider returning true
                return false;
            }
        }

        protected void CleanupSession()
        {
            this.networkStream?.Dispose();
            this.tcpClient?.Close();

            this.networkStream = null;
            this.tcpClient = null;
        }

        protected THistoryEntry Send(string message)
        {
            var historyEntry = new RequestResponseContainer();
            if (!string.IsNullOrEmpty(message))
            {
                var messageBytes = Encoding.UTF8.GetBytes(message);
                CommonMailHelper.ResizeAndComplementWithANewLineIfNecessary(ref messageBytes);

                this.loadTimeStopwatch.Restart();
                this.networkStream.Write(messageBytes, 0, messageBytes.Length);
                this.networkStream.Flush();
                this.loadTimeStopwatch.Stop();

                historyEntry.Request = message;
                historyEntry.RequestSendTime = this.loadTimeStopwatch.Elapsed;
            }

            this.loadTimeStopwatch.Restart();
            var responseBytes = this.Read();
            this.loadTimeStopwatch.Stop();

            historyEntry.Response = Encoding.UTF8.GetString(responseBytes);
            historyEntry.ResponseReceiveTime = this.loadTimeStopwatch.Elapsed;

            var modifiedEntry = this.AddHistoryEntry(historyEntry);
            return modifiedEntry;
        }

        protected byte[] Read()
        {
            this.ResetReadDependencies();

            while (true)
            {
                read = this.networkStream.Read(this.buffer, 0, bufferSize);
                if (read > 0)
                {
                    if (currentReadOffset + read > this.currentRead.Length)
                        Array.Resize(ref this.currentRead, this.currentRead.Length + bufferSize);

                    Array.Copy(this.buffer, 0, this.currentRead, this.currentReadOffset, read);
                    this.currentReadOffset += read;
                }

                if (!this.networkStream.DataAvailable
                    && this.read != 0 // TODO: reconsider
                    && CommonMailHelper.EndsWithANewLine(buffer)
                    && this.ReadCompletionVerification())
                    break;
            }

            if (this.currentRead.Length != this.currentReadOffset)
                Array.Resize(ref this.currentRead, this.currentReadOffset);

            return this.currentRead;
        }

        protected void ResetReadDependencies()
        {
            Array.Clear(this.buffer, 0, bufferSize);
            Array.Resize(ref this.currentRead, bufferSize);
            this.currentReadOffset = 0;
            this.read = 0;
        }

        protected bool AreCredentialsNew(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(this.email))
                return true;

            if (string.IsNullOrEmpty(this.password))
                return true;

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null, empty or whitespace.", nameof(email));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty.", nameof(password));

            return !(this.email.Equals(email.Trim().ToLowerInvariant()) && this.password == password);
        }

        protected THistoryEntry AddHistoryEntry(RequestResponseContainer historyEntry)
        {
            var modifiedEntry = this.ModifyHistoryEntryOnAdd(historyEntry);
            if (modifiedEntry != null)
                this.history.Add(modifiedEntry);
            return modifiedEntry;
        }

        protected virtual bool ReadCompletionVerification()
        {
            return true;
        }
        
        protected abstract void SendAuthenticationRequest(string email, string password);

        protected abstract string CreateSessionShutdownMessage();

        protected abstract THistoryEntry ModifyHistoryEntryOnAdd(RequestResponseContainer historyEntry);

        #region IDisposable Support
        private volatile bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.CleanupSession();
                }

                this.networkStream = null;
                this.tcpClient = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
