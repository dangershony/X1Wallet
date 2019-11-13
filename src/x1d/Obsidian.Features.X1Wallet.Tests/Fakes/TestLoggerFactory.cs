using System;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Obsidian.Features.X1Wallet.Tests.Fakes
{
    class TestLoggerFactory : ILoggerFactory
    {
        readonly ITestOutputHelper testOutputHelper;

        public TestLoggerFactory(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            throw new NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, this.testOutputHelper);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
