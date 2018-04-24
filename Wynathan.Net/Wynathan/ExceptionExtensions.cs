using System;
using System.Text;

namespace Wynathan
{
    public static class ExceptionExtensions
    {
        public static string Format(this Exception ex)
        {
            if (ex == null)
                return string.Empty;

            var currentMessage = string.Format("{0}{1}{2}{1}", ex.Message,
                Environment.NewLine, Format(ex.InnerException));

            var aggrException = ex as AggregateException;
            if (aggrException != null)
            {
                var aggrMessageBuilder = new StringBuilder();
                aggrMessageBuilder.Append(currentMessage);

                foreach (var inner in aggrException.InnerExceptions)
                {
                    aggrMessageBuilder.Append(string.Format("{0}{1}{2}{1}",
                        inner.Message, Environment.NewLine, Format(inner.InnerException)));
                }

                return aggrMessageBuilder.ToString();
            }

            return currentMessage;
        }
    }
}
