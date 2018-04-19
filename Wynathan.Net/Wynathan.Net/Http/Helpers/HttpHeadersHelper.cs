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

        internal const string RFC2616MultipleFieldValueHeaderSeparator = ", ";

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

        /// <summary>
        /// Attempts to retrieve the value of an HTTP header specified by its 
        /// <paramref name="header"/> reference.
        /// </summary>
        /// <param name="headers">
        /// A collection of HTTP headers to search within of.
        /// </param>
        /// <param name="header">
        /// An <see cref="HttpRequestHeader"/> representation of an HTTP header to 
        /// look up.
        /// </param>
        /// <param name="value">
        /// The <see cref="string"/> output value of the respective <paramref name="header"/>.
        /// </param>
        /// <returns>
        /// True, if successfully retrieved <paramref name="header"/>'s value. 
        /// Otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Attempts to retrieve the value of an HTTP header specified by its 
        /// <paramref name="header"/> reference.
        /// </summary>
        /// <param name="headers">
        /// A collection of HTTP headers to search within of.
        /// </param>
        /// <param name="header">
        /// An <see cref="HttpRequestHeader"/> representation of an HTTP header to 
        /// look up.
        /// </param>
        /// <param name="value">
        /// The <see cref="int"/> output value of the respective <paramref name="header"/>.
        /// </param>
        /// <returns>
        /// True, if successfully retrieved and parsed to <see cref="int"/> 
        /// <paramref name="header"/>'s value. Otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Converts a <see cref="byte"/> collection to an array of <see cref="string"/>s 
        /// that represent HTTP headers. Does not adjust headers' values if those are 
        /// separated by CRLF (consider <see cref="ParseHeaders(string[], bool)"/> on 
        /// that matter).
        /// </summary>
        /// <param name="headers">
        /// A collection of bytes that represent HTTP headers.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// In case received <see cref="headers"/> do not contain CRLFCRLF at the end.
        /// </exception>
        public static string[] GetHeaders(IList<byte> headers)
        {
            int indexAfterDoubleCrLf;
            if (!ContainsDoubleCrLf(headers, out indexAfterDoubleCrLf) || indexAfterDoubleCrLf != headers.Count)
                throw new ArgumentException("Received headers do not contain double CRLF at the end.", nameof(headers));

            // Headers should be in UTF-8.
            var headersResult = Encoding.UTF8.GetString(headers.ToArray())
                .Split(newLineSeparators, StringSplitOptions.RemoveEmptyEntries);
            return headersResult;
        }

        /// <summary>
        /// Looks up an HTTP status code header within the <paramref name="headers"/> 
        /// collection, and returns its <see cref="HttpStatusCode"/> equivalent.
        /// </summary>
        /// <param name="headers">
        /// A headers collection to search HTTP status code header within of.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// If unable to find an HTTP status code header in <paramref name="headers"/>.
        /// </exception>
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

            throw new ArgumentException("Unable to find HTTP status code header.", nameof(headers));
        }

        /// <summary>
        /// Parses input <paramref name="headers"/> into a key-value pair collection.
        /// </summary>
        /// <param name="headers">
        /// An array to parse into dictionary.
        /// </param>
        /// <param name="excludeSetCookie">
        /// Specifies whether to exclude Set-Cookie header from resulting dictionary, 
        /// or to keep it.
        /// </param>
        /// <returns></returns>
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
                        // Considering CRLF as a separator within the header's value, the previous 
                        // value line should contain implicit separator (e.g., ',' (comma)) on its own.
                        result[previousKey] = string.Join(" ", result[previousKey], value);
                    }
                }
            }

            if (excludeSetCookie)
            {
                // TODO: optimise
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

        /// <summary>
        /// Verifies whether <paramref name="buffer"/> containes two consecutive carriage 
        /// return and line feed pairs (CRLFCRLF), and specifies the first index in the 
        /// <paramref name="buffer"/> that follows those pairs, if any.
        /// </summary>
        /// <param name="buffer">
        /// A collection to check for CRLFCRLF.
        /// </param>
        /// <param name="endIndex">
        /// The first index in the <paramref name="buffer"/> that follows CRLFCRLF. Can 
        /// be equal to <paramref name="buffer"/>'s length if CRLFCRLF terminate the 
        /// <paramref name="buffer"/>, or to -1 if no CRLFCRLF found.
        /// </param>
        /// <returns>
        /// True, if <paramref name="buffer"/> contains CRLFCRLF. Otherwise, false.
        /// </returns>
        public static bool ContainsDoubleCrLf(IList<byte> buffer, out int endIndex)
        {
            for (var i = 0; i <= buffer.Count - 4; i++)
            {
                if (buffer[i] == 13 && buffer[i + 1] == 10 && buffer[i + 2] == 13 && buffer[i + 3] == 10)
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
