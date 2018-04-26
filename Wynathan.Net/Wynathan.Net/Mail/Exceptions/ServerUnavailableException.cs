using System;
using Wynathan.Net.Mail.Models;

namespace Wynathan.Net.Mail.Exceptions
{

    [Serializable]
    public class ServerUnavailableException : Exception
    {
        internal ServerUnavailableException(string host, MailPort port, RequestResponseContainer response)
            : base(string.Format("Server {0}:{1} is currently unavailable. Response received: {2}", host, (int)port, response.Response)) { }

        internal ServerUnavailableException(string message) 
            : base(message) { }

        internal ServerUnavailableException(string message, Exception inner) 
            : base(message, inner) { }

        protected ServerUnavailableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
