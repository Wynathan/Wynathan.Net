using System;
using System.Runtime.Serialization;

using Wynathan.Net.Mail.Models;

namespace Wynathan.Net.Mail.Exceptions
{
    [Serializable]
    public class MailboxAutheticationException : Exception
    {
        internal MailboxAutheticationException(string message) 
            : base(message) { }

        internal MailboxAutheticationException(string host, MailPort port, string email, RequestResponseContainer response)
            : base(string.Format("An error occurred on an attempt to authenticate on remote host {0}:{1} using credentials provided for <{2}>. Response received: {3}.", 
                host, (int)port, email, response.Response)) { }

        internal MailboxAutheticationException(string message, Exception inner) 
            : base(message, inner) { }

        protected MailboxAutheticationException(SerializationInfo info, StreamingContext context) 
            : base(info, context) { }
    }
}
