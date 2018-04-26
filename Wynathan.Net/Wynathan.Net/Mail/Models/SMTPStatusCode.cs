namespace Wynathan.Net.Mail.Models
{
    /// <summary>
    /// SMTP status codes as specified in RFC-821, section 
    /// 4.2.2. NUMERIC ORDER LIST OF REPLY CODES
    /// </summary>
    /// <seealso cref="https://tools.ietf.org/html/rfc821"/>
    public enum SMTPStatusCode
    {
        None = -1,

        SystemHelp = 211,
        HelpMessage = 214,
        ConnectionEstablished = 220,
        ConnectionShuttingDown = 221,
        // This one is from auth extension - https://tools.ietf.org/html/rfc4954
        // TODO: adjust comments
        AuthenticationSuccessful = 235,
        Ok = 250,
        Forwarding = 251,
        // This one is from auth extension - https://tools.ietf.org/html/rfc4954
        // TODO: adjust comments
        AwaitingAuthData = 334,
        AwaitingMailData = 354,
        // TODO: lookup where this came from; M2W supports this one
        UnknownError = 405,
        NotAvailableAndShuttingDown = 421,
        MailActionUnavailable = 450,
        ErrorInProcessing = 451,
        InsufficientSystemStorage = 452,
        SyntaxError = 500,
        InvalidParameters = 501,
        CommandNotImplemented = 502,
        BadCommandSequence = 503,
        CommandParameterNotImplemented = 504,
        MailboxUnavailable = 550,
        TryForward = 551,
        ExceededStorageAllocation = 552,
        InvalidMailboxName = 553,
        TransactionFailed = 554
    }
}
