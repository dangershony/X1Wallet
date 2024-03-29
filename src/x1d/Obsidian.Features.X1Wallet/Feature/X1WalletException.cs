﻿using System;
using System.Net;

namespace Obsidian.Features.X1Wallet.Feature
{
    public sealed class X1WalletException : Exception
    {
        public readonly HttpStatusCode HttpStatusCode;

        public X1WalletException(string message) : this(HttpStatusCode.BadRequest, message)
        {
        }

        public X1WalletException(HttpStatusCode httpStatusCode, string message, Exception innerException = null) : base(message, innerException)
        {
            this.HttpStatusCode = httpStatusCode;
        }

        public override string ToString()
        {
            return $"Error {this.HttpStatusCode}: {base.ToString()}";
        }
    }
}
