using System;

namespace Wynathan.Net.Mail.Exceptions
{
    [Serializable]
    public class UnexpectedServerResponseException : Exception
    {
        internal UnexpectedServerResponseException()
            : base("Received an invalid/unexpected server response token.") { }

        internal UnexpectedServerResponseException(string message) 
            : base(message) { }

        internal UnexpectedServerResponseException(string message, Exception inner) 
            : base(message, inner) { }

        protected UnexpectedServerResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
