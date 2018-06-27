using System;
using System.Runtime.Serialization;

namespace Wynathan.Net.Mail.Exceptions
{

    [Serializable]
    public class RejectedMailboxException : Exception
    {
        public RejectedMailboxException(string mailboxName) 
            : base(string.Format("Mailbox <{0}> was rejected by the server.", mailboxName)) { }

        protected RejectedMailboxException(
            SerializationInfo info,
            StreamingContext context) 
            : base(info, context) { }
    }
}
