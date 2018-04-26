using System;
using System.Text;

using Wynathan.Net.Mail.Exceptions;
using Wynathan.Net.Mail.Helpers;
using Wynathan.Net.Mail.Models;

namespace Wynathan.Net.Mail
{
    public sealed class SMTPClient : MailClientBase<SMTPRequestResponseContainer>
    {
        public SMTPClient(string host, bool useTls = false)
            : base(host, MailPort.SMTP) // TODO: consider varying port based on useTls parameter
        {

        }

        public SMTPRequestResponseContainer SendEmail()
        {
            // TODO: message may contain "from" header on its own already; 
            // consider to check if it is consistent with the one passed 
            // as a parameter; consider to check consistency with the auth 
            // email as well.
            // TODO: consider multiple recipients and a case when a message 
            // should be assembled from a scratch (from, to, cc, bcc, etc.)
            
        }

        protected override void SendAuthenticationRequest(string email, string password)
        {
            var helloResponse = this.Send("helo");
            if (helloResponse.StatusCode != SMTPStatusCode.ConnectionEstablished)
                throw new ServerUnavailableException(this.host, this.port, helloResponse);

            var authResponse = this.Send("auth");
            if (authResponse.StatusCode != SMTPStatusCode.AwaitingAuthData)
                throw new UnexpectedServerResponseException();

            var emailBytes = Encoding.UTF8.GetBytes(email);
            var base64Email = Convert.ToBase64String(emailBytes);
            var usernameResponse = this.Send(base64Email);
            if (usernameResponse.StatusCode != SMTPStatusCode.AwaitingAuthData)
                throw new UnexpectedServerResponseException();

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var base64Password = Convert.ToBase64String(passwordBytes);
            var passwordResponse = this.Send(base64Password);
            if (passwordResponse.StatusCode != SMTPStatusCode.AuthenticationSuccessful)
                throw new MailboxAutheticationException(this.host, this.port, email, helloResponse);
        }

        protected override string CreateSessionShutdownMessage()
        {
            return "quit";
        }

        protected override SMTPRequestResponseContainer ModifyHistoryEntryOnAdd(RequestResponseContainer historyEntry)
        {
            return new SMTPRequestResponseContainer(historyEntry);
        }

        private static void ResizeAndTerminateMessageIfNecessary(byte[] message)
        {
            var length = message.Length;
            if (message[length - 5] == CommonMailHelper.ByteCarriageReturn
                && message[length - 4] == CommonMailHelper.ByteNewLine
                && message[length - 3] == CommonMailHelper.ByteDot
                && message[length - 2] == CommonMailHelper.ByteCarriageReturn
                && message[length - 1] == CommonMailHelper.ByteNewLine)
                return;

            if (message[length - 4] == CommonMailHelper.ByteCarriageReturn
                && message[length - 3] == CommonMailHelper.ByteNewLine
                && message[length - 2] == CommonMailHelper.ByteDot
                && message[length - 1] == CommonMailHelper.ByteCarriageReturn)
            {
                Array.Resize(ref message, length + 1);
                message[length] = CommonMailHelper.ByteNewLine;
                return;
            }

            if (message[length - 3] == CommonMailHelper.ByteCarriageReturn
                && message[length - 2] == CommonMailHelper.ByteNewLine
                && message[length - 1] == CommonMailHelper.ByteDot)
            {
                Array.Resize(ref message, length + 2);
                message[length] = CommonMailHelper.ByteCarriageReturn;
                message[length + 1] = CommonMailHelper.ByteNewLine;
                return;
            }

            if (CommonMailHelper.EndsWithANewLine(message))
            {
                Array.Resize(ref message, length + 3);
                message[length] = CommonMailHelper.ByteDot;
                message[length + 1] = CommonMailHelper.ByteCarriageReturn;
                message[length + 2] = CommonMailHelper.ByteNewLine;
                return;
            }

            if (message[length - 1] == CommonMailHelper.ByteCarriageReturn)
            {
                Array.Resize(ref message, length + 4);
                message[length] = CommonMailHelper.ByteNewLine;
                message[length + 1] = CommonMailHelper.ByteDot;
                message[length + 2] = CommonMailHelper.ByteCarriageReturn;
                message[length + 3] = CommonMailHelper.ByteNewLine;
                return;
            }

            // TODO: consider Linux users that may just ignore the fact that 
            // RFC requires the message to be complemented with \r\n, and not 
            // just with \n.

            Array.Resize(ref message, length + 5);
            message[length] = CommonMailHelper.ByteCarriageReturn;
            message[length + 1] = CommonMailHelper.ByteNewLine;
            message[length + 2] = CommonMailHelper.ByteDot;
            message[length + 3] = CommonMailHelper.ByteCarriageReturn;
            message[length + 4] = CommonMailHelper.ByteNewLine;
        }
    }
}
