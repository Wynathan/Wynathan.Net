using System;
using System.Text;

namespace Wynathan.Net.Mail.Models
{
    public class RequestResponseContainer
    {
        private string messageString;
        private byte[] message;

        public static Encoding ConvertionEncoding = Encoding.UTF8;

        public string RequestString
        {
            get
            {
                return this.messageString;
            }
            internal set
            {
                this.messageString = value;
                if (string.IsNullOrEmpty(value))
                    this.message = null;
                else
                    this.message = ConvertionEncoding.GetBytes(value);
            }
        }

        public byte[] Request
        {
            get
            {
                return this.message;
            }
            internal set
            {
                this.message = value;
                if (value.IsNullOrEmpty())
                    this.messageString = null;
                else
                    this.messageString = ConvertionEncoding.GetString(value);
            }
        }

        public TimeSpan RequestSendTime { get; internal set; }

        public string Response { get; internal set; }

        public TimeSpan ResponseReceiveTime { get; internal set; }
    }
}
