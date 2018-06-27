using System;
using System.Runtime.Serialization;

namespace Wynathan.Net.Mail.Exceptions
{
    [Serializable]
    public class AuthenticationRequiredException : Exception
    {
        public AuthenticationRequiredException()
            : base("Unable to commence communications with the email server without preliminarily authenticating.") { }

        protected AuthenticationRequiredException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context) { }
    }
}
