using System;
using System.Runtime.Serialization;

namespace Wynathan.Net.Mail.Exceptions
{
    [Serializable]
    public class UnexpectedServerResponseException : Exception
    {
        internal UnexpectedServerResponseException()
            : base("Received an invalid/unexpected server response token.") { }

        internal UnexpectedServerResponseException(Enum value)
            : base(string.Format("Received an unexpected server response token of {0} ({1}).", Convert.ToInt32(value), value.ToString())) { }

        internal UnexpectedServerResponseException(Enum value, string command)
            : base(string.Format("Received an unexpected server response token of {0} ({1}) during \"{2}\" command.", Convert.ToInt32(value), value.ToString(), command)) { }

        internal UnexpectedServerResponseException(string response) 
            : base(string.Format("Received an invalid/unexpected server response token: \"{0}\"", response)) { }

        internal UnexpectedServerResponseException(string message, Exception inner) 
            : base(message, inner) { }

        protected UnexpectedServerResponseException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context) { }
    }
}
