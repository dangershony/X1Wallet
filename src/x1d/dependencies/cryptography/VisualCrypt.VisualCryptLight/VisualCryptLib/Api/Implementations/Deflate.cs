﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace VisualCrypt.VisualCryptLight.VisualCryptLib.Api.Implementations
{
    public static class Deflate
    {
        public static byte[] Compress(string text, Encoding encoding)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            if (encoding == null)
                throw new ArgumentNullException("encoding");

            var bytes = encoding.GetBytes(text);

            using (var inputStream = new MemoryStream(bytes))
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(outputStream, CompressionMode.Compress))
                {
                    inputStream.CopyTo(deflateStream);

                }
                return outputStream.ToArray();
            }
        }

        public static byte[] CompressBytes(byte[] bytes)
        {
            using (var inputStream = new MemoryStream(bytes))
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(outputStream, CompressionMode.Compress))
                {
                    inputStream.CopyTo(deflateStream);

                }
                return outputStream.ToArray();
            }
        }

        public static string Decompress(byte[] data, Encoding encoding)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (encoding == null)
                throw new ArgumentNullException("encoding");

            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(outputStream);
                    return encoding.GetString(outputStream.ToArray(), 0, (int)outputStream.Length);
                }
            }
        }

        public static byte[] DecompressBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
        }
    }
}