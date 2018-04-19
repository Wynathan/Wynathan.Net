using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Wynathan.Net.Extensions;

namespace Wynathan.Net.Http.Helpers
{
    internal static class HttpBodyHelper
    {
        /// <summary>
        /// Verifies whether the <paramref name="body"/> is a valid 
        /// chunked body as per RFC-2616, section 3.6.1 Chunked Transfer Coding.
        /// </summary>
        /// <param name="body">
        /// A list of bytes to verify for chunked coding validity.
        /// </param>
        /// <returns>
        /// True if valid. Otherwise, false.
        /// </returns>
        public static bool IsValidChunkedBody(IList<byte> body)
        {
            var sizeList = new List<byte>();
            // Key-index/Value-size;
            var chunks = new Dictionary<int, int>();
            for (int i = 0; i < body.Count; i++)
            {
                if (body[i] == 13 && i + 1 >= body.Count)
                    return false;

                if (body[i] == 13 && body[i + 1] == 10)
                {
                    var hex = Encoding.UTF8.GetString(sizeList.ToArray());
                    int size = int.Parse(hex, NumberStyles.HexNumber);
                    chunks.Add(i + 2, size);
                    sizeList.Clear();
                    // + \r\n + size + \r\n - current loop iteration
                    i += 2 + size + 2 - 1;
                    continue;
                }

                sizeList.Add(body[i]);
            }
            
            // TODO: reconsider; use no Select and consider default(KeyValuePair<int, int>).Equals(last)
            var last = chunks.Select(x => new { Key = x.Key, Value = x.Value })
                .OrderByDescending(x => x.Key)
                .FirstOrDefault();

            if (last == null)
                return false;

            return last.Value == 0;
        }

        /// <summary>
        /// Parses <paramref name="body"/> to an adjusted readable HTTP response 
        /// body using <paramref name="headers"/> to make appropriate adjustments.
        /// </summary>
        /// <param name="headers">
        /// HTTP headers to be used to identify adjustments required to parse the 
        /// <paramref name="body"/>.
        /// </param>
        /// <param name="body">
        /// The HTTP response body to parse.
        /// </param>
        /// <param name="bytes">
        /// Changes this value to the one specified in Content-Length header if any.
        /// </param>
        /// <returns>
        /// UTF-8 encoded readable string.
        /// </returns>
        public static string GetBody(Dictionary<string, string> headers, byte[] body, ref long bytes)
        {
            // TODO: bytes var is not being set if Content-Length is not provided; verify if 
            // such behaviour is valid.
            // TODO: current implementation disregards content encoding, using UTF-8 by default.
            if (body.IsNullOrEmpty())
                return null;

            var contentLengthHeaderKey = headers.Keys.FirstOrDefault(x => x.EqualsII("Content-Length"));
            if (!string.IsNullOrWhiteSpace(contentLengthHeaderKey))
            {
                var lengthVal = headers[contentLengthHeaderKey];
                int length;
                if (int.TryParse(lengthVal, out length))
                    bytes = length;
            }

            var magic = body;

            // Transfer-Encoding: chunked
            var transferEncodingHeaderKey = headers.Keys.FirstOrDefault(x => x.EqualsII("Transfer-Encoding"));
            if (!string.IsNullOrWhiteSpace(transferEncodingHeaderKey))
            {
                var transferEncoding = headers[transferEncodingHeaderKey];
                if (transferEncoding.EqualsII("chunked"))
                    magic = ReadChunkedBodyData(body);
            }

            var encodingHeaderKey = headers.Keys.FirstOrDefault(x => x.EqualsII("Content-Encoding"));
            if (string.IsNullOrWhiteSpace(encodingHeaderKey))
                return Encoding.UTF8.GetString(magic);

            var encoding = headers[encodingHeaderKey]?.ToLowerInvariant().Trim();

            using (var ms = new MemoryStream(magic))
            {
                switch (encoding)
                {
                    case "gzip":
                        using (var stream = new GZipStream(ms, CompressionMode.Decompress, false))
                            return ReadFromStream(stream);
                    case "deflate":
                        using (var stream = new DeflateStream(ms, CompressionMode.Decompress, false))
                            return ReadFromStream(stream);
                    default:
                        // TODO: reconsider
                        throw new NotImplementedException($"Content-Encoding header is {encoding}.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        /// <seealso cref="https://tools.ietf.org/html/rfc7230#section-3.3.1"/>
        private static byte[] ReadChunkedBodyData(byte[] body)
        {
            var sizeList = new List<byte>();
            // Key-index/Value-size;
            var chunks = new Dictionary<int, int>();
            for (int i = 0; i < body.Length; i++)
            {
                if (body[i] == 13 && body[i + 1] == 10)
                {
                    var hex = Encoding.UTF8.GetString(sizeList.ToArray());
                    int size = int.Parse(hex, NumberStyles.HexNumber);
                    chunks.Add(i + 2, size);
                    sizeList.Clear();
                    // + \r\n + size + \r\n - current loop iteration
                    i += 2 + size + 2 - 1;
                    continue;
                }

                sizeList.Add(body[i]);
            }

            var result = new List<byte>();
            foreach (var chunk in chunks.OrderBy(x => x.Key))
            {
                var c = body.Skip(chunk.Key).Take(chunk.Value);
                result.AddRange(c);
            }

            return result.ToArray();
        }

        private static string ReadFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
