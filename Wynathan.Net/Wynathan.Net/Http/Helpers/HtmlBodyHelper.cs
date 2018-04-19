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
    internal static class HtmlBodyHelper
    {
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

            var last = chunks.Select(x => new { Key = x.Key, Value = x.Value })
                .OrderBy(x => x.Key)
                .LastOrDefault();

            if (last == null)
                return false;

            return last.Value == 0;
        }

        public static string GetHtml(Dictionary<string, string> headers, byte[] body, ref long bytes)
        {
            if (body.IsNullOrEmpty())
                return null;

            var contentLengthHeaderKey = headers.Keys.FirstOrDefault(x => x.StartsWithII("Content-Length"));
            if (!string.IsNullOrWhiteSpace(contentLengthHeaderKey))
            {
                var lengthVal = headers[contentLengthHeaderKey];
                int length;
                if (int.TryParse(lengthVal, out length))
                    bytes = length;
            }

            var magic = body;

            // Transfer-Encoding: chunked
            var transferEncodingHeaderKey = headers.Keys.FirstOrDefault(x => x.StartsWithII("Transfer-Encoding"));
            if (!string.IsNullOrWhiteSpace(transferEncodingHeaderKey))
            {
                var transferEncoding = headers[transferEncodingHeaderKey];
                if (transferEncoding.EqualsII("chunked"))
                    magic = ReadChunkedHtmlData(body);
            }

            var encodingHeaderKey = headers.Keys.FirstOrDefault(x => x.StartsWithII("Content-Encoding"));
            if (string.IsNullOrWhiteSpace(encodingHeaderKey))
                return Encoding.UTF8.GetString(magic);

            var encoding = headers[encodingHeaderKey];

            using (var ms = new MemoryStream(magic))
            {
                if (encoding.EqualsII("gzip"))
                {
                    using (var stream = new GZipStream(ms, CompressionMode.Decompress, false))
                        return ReadFromStream(stream);
                }
                else if (encoding.EqualsII("deflate"))
                {
                    using (var stream = new DeflateStream(ms, CompressionMode.Decompress, false))
                        return ReadFromStream(stream);
                }
            }

            // TODO: gotta do smth with it; not to live it like this, duh.
            throw new NotImplementedException($"Content-Encoding header is {encoding}.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        /// <seealso cref="https://tools.ietf.org/html/rfc7230#section-3.3.1"/>
        private static byte[] ReadChunkedHtmlData(byte[] body)
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
