using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wynathan.Net.Mail.Helpers
{
    internal static class CommonMailHelper
    {
        public const byte ByteCarriageReturn = (byte)'\r';
        public const byte ByteNewLine = (byte)'\n';
        public const byte ByteDot = (byte)'.';

        public static bool EndsWithANewLine(byte[] message)
        {
            return message[message.Length - 2] == CommonMailHelper.ByteCarriageReturn
                && message[message.Length - 1] == CommonMailHelper.ByteNewLine;
        }

        public static bool ResizeAndComplementWithANewLineIfNecessary(ref byte[] message)
        {
            if (EndsWithANewLine(message))
                return false;

            var length = message.Length;
            if (message[length - 1] == CommonMailHelper.ByteCarriageReturn)
            {
                Array.Resize(ref message, length + 1);
                message[length] = CommonMailHelper.ByteNewLine;
                return true;
            }

            Array.Resize(ref message, length + 2);
            message[length] = CommonMailHelper.ByteCarriageReturn;
            message[length + 1] = CommonMailHelper.ByteNewLine;
            return true;
        }
    }
}
