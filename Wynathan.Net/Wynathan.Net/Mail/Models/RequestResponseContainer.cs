using System;

namespace Wynathan.Net.Mail.Models
{
    public class RequestResponseContainer
    {
        public string Request { get; internal set; }
        public TimeSpan RequestSendTime { get; internal set; }

        public string Response { get; internal set; }
        public TimeSpan ResponseReceiveTime { get; internal set; }
    }
}
