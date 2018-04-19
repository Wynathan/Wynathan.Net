using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using Wynathan.Net.Extensions;

namespace Wynathan.Net.Http.Helpers
{
    internal static class HttpHeadersHelper
    {
        public const string HeaderSetCookie = "Set-Cookie";

        private const string RFC2616MultipleFieldValueHeaderSeparator = ", ";

        private static readonly char[] headerNameValueSeparators = new char[] { ':' };
        private static readonly string[] newLineSeparators = new[] { "\r\n" };

        private const string headerNamePattern = "^[\\w-]+$";
        private static readonly Regex headerNameRegex = new Regex(headerNamePattern);

        private const string httpHeaderPatternStatusCodeName = "statuscode";
        private const string httpHeaderPattern = "^HTTP/[\\d\\.]+ (?<" + httpHeaderPatternStatusCodeName + ">\\d+).*$";
        private static readonly Regex httpHeaderRegex = new Regex(httpHeaderPattern);

        private static string GetHeaderNameByType(HttpRequestHeader header)
        {
            switch (header)
            {
                case HttpRequestHeader.ContentLocation:
                    return "Location";
                case HttpRequestHeader.Connection:
                    return "Connection";
                case HttpRequestHeader.KeepAlive:
                    return "Keep-Alive";
                case HttpRequestHeader.ContentLength:
                    return "Content-Length";
                case HttpRequestHeader.TransferEncoding:
                    return "Transfer-Encoding";
                case HttpRequestHeader.ContentType:
                    return "Content-Type";
                case HttpRequestHeader.Cookie:
                    return HeaderSetCookie;
                default:
                    throw new NotImplementedException("Requested header is not supported yet.");
            }
        }

        public static bool TryRetrieveHeaderValue(string[] headers, HttpRequestHeader header, out string value)
        {
            var headersParsed = ParseHeaders(headers, false);
            var requestedHeaderName = GetHeaderNameByType(header).Trim();
            var key = headersParsed.Keys.FirstOrDefault(x => requestedHeaderName.EqualsII(x));
            if (string.IsNullOrWhiteSpace(key))
            {
                value = null;
                return false;
            }
            else
            {
                value = headersParsed[key];
                return true;
            }
        }

        public static bool TryRetrieveHeaderValue(string[] headers, HttpRequestHeader header, out int value)
        {
            value = -1;

            string val;
            if (TryRetrieveHeaderValue(headers, header, out val))
            {
                if (int.TryParse(val, out value))
                    return true;
            }

            return false;
        }

        public static string[] GetHeaders(IList<byte> headers)
        {
            int indexAfterDoubleCrLf;
            if (!ContainsDoubleCrLf(headers, out indexAfterDoubleCrLf) || indexAfterDoubleCrLf != headers.Count)
                throw new InvalidOperationException("Received headers do not contain double CRLF at the end.");

            var headersResult = Encoding.UTF8.GetString(headers.ToArray())
                .Split(newLineSeparators, StringSplitOptions.RemoveEmptyEntries);
            return headersResult;
        }

        public static HttpStatusCode GetStatusCode(string[] headers)
        {
            // HTTP status code header should be the first, so it is the same 
            // as call headers.First, although let us be sure.
            foreach (var header in headers)
            {
                var match = httpHeaderRegex.Match(header);
                if (match.Success)
                {
                    var group = match.Groups[httpHeaderPatternStatusCodeName].Value;
                    var value = int.Parse(group);
                    return (HttpStatusCode)value;
                }
            }

            throw new InvalidOperationException("Unable to find HTTP status code header.");
        }

        public static Dictionary<string, string> ParseHeaders(string[] headers, bool excludeSetCookie = true)
        {
            var result = new Dictionary<string, string>();
            string previousKey = null;
            for (int i = 0; i < headers.Length; i++)
            {
                var kvp = headers[i].Split(headerNameValueSeparators, 2);
                var name = kvp[0];
                if (kvp.Length == 2 && headerNameRegex.IsMatch(name))
                {
                    var value = kvp[1].Trim();
                    var entryKey = result.Keys.FirstOrDefault(x => x.EqualsII(name));
                    if (string.IsNullOrWhiteSpace(entryKey))
                    {
                        result[name] = value;
                        previousKey = name;
                    }
                    else
                    {
                        result[entryKey] = string.Join(RFC2616MultipleFieldValueHeaderSeparator, result[entryKey], value);
                        previousKey = entryKey;
                    }
                }
                else
                {
                    // Can be true for HTTP status header
                    if (!string.IsNullOrWhiteSpace(previousKey))
                    {
                        var value = headers[i].Trim();
                        result[previousKey] = string.Join(" ", result[previousKey], value);
                    }
                }
            }

            if (excludeSetCookie)
            {
                var keysToRemove = new List<string>();
                foreach (var key in result.Keys)
                {
                    if (HeaderSetCookie.EqualsII(key))
                        keysToRemove.Add(key);
                }

                foreach (var key in keysToRemove)
                    result.Remove(key);
            }

            return result;
        }

        public static bool ContainsDoubleCrLf(IList<byte> buffer, out int endIndex)
        {
            for (var i = 0; i <= buffer.Count - 4; i++)
            {
                if (buffer[i] == 13 & buffer[i + 1] == 10 && buffer[i + 2] == 13 && buffer[i + 3] == 10)
                {
                    endIndex = i + 4;
                    return true;
                }
            }
            endIndex = -1;
            return false;
        }
    }
}
