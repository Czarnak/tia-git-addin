using System;
using System.Linq;
using System.Text;

namespace TiaGitAddIn.Tests.Revision
{
    /// <summary>
    /// Builds raw byte fixtures for the encodings <see cref="TiaGitAddIn.Services.Revision.PlcRevisionProvider"/>
    /// is required to decode: UTF-8 with and without a BOM, and UTF-16 LE/BE with a mandatory BOM.
    /// </summary>
    internal static class EncodingFixture
    {
        public static byte[] Create(string fixture, string text)
        {
            switch (fixture)
            {
                case "utf8":
                    return new UTF8Encoding(false).GetBytes(text);
                case "utf8-bom":
                    return Prefix(new byte[] { 0xEF, 0xBB, 0xBF }, new UTF8Encoding(false).GetBytes(text));
                case "utf16-le":
                    return Prefix(new byte[] { 0xFF, 0xFE }, Encoding.Unicode.GetBytes(text));
                case "utf16-be":
                    return Prefix(new byte[] { 0xFE, 0xFF }, Encoding.BigEndianUnicode.GetBytes(text));
                default:
                    throw new ArgumentException($"Unknown encoding fixture '{fixture}'.", nameof(fixture));
            }
        }

        private static byte[] Prefix(byte[] prefix, byte[] payload) => prefix.Concat(payload).ToArray();
    }
}
