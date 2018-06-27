namespace Wynathan.Net.Mail.Models
{
    public sealed class SMTPRequestResponseContainer : RequestResponseContainer
    {
        internal SMTPRequestResponseContainer(RequestResponseContainer container)
        {
            this.RequestString = container.RequestString;
            this.RequestSendTime = container.RequestSendTime;
            this.Response = container.Response;
            this.ResponseReceiveTime = container.ResponseReceiveTime;
            var statusCodeRaw = container.Response?.Substring(0, 4).Trim();
            int statusCode;
            if (!string.IsNullOrWhiteSpace(statusCodeRaw) && int.TryParse(statusCodeRaw, out statusCode))
                this.StatusCode = (SMTPStatusCode)statusCode;
            else
                this.StatusCode = SMTPStatusCode.None;
        }

        public SMTPStatusCode StatusCode { get; private set; }
    }
}
