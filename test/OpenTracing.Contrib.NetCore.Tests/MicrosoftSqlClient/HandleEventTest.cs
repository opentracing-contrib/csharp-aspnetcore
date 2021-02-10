using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.MicrosoftSqlClient;
using OpenTracing.Mock;
using OpenTracing.Noop;
using Xunit;

namespace OpenTracing.Contrib.NetCore.Tests.MicrosoftSqlClient
{
    public class HandleEventTest
    {
        private readonly IObserver<KeyValuePair<string, object>> _microsoftSqlClientDiagnostics;

        public HandleEventTest()
        {
            _microsoftSqlClientDiagnostics = new MicrosoftSqlClientDiagnostics(
                NullLoggerFactory.Instance,
                NoopTracerFactory.Create(),
                Options.Create(new MicrosoftSqlClientDiagnosticOptions {StartRootSpans = false}));
        }

        [Fact]
        async Task CanHandleTwoDifferentTypesOfWriteCommandBeforeInParallel()
        {
            const string eventName = MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandBefore;
            var command1 = new {Command = new SqlCommand("Insert into"), Id = Guid.NewGuid()};
            var command2 = new {Command = new SqlCommand("Update where"), Id = Guid.NewGuid(), ExtraProp = "any value"};
            var kv1 = new KeyValuePair<string, object>(eventName, command1);
            var kv2 = new KeyValuePair<string, object>(eventName, command2);

            var tasks1 = Enumerable.Range(0, 100)
                .Select(i => Task.Run(() => _microsoftSqlClientDiagnostics.OnNext(i % 2 == 0 ? kv1 : kv2)));

            await Task.WhenAll(tasks1);
        }
        
        [Fact]
        void CanHandleWriteCommandAfter()
        {
            const string eventNameBefore = MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandBefore;
            var commandBefore = new {Command = new SqlCommand("Insert into"), Id = Guid.NewGuid()};
            var kvBefore = new KeyValuePair<string, object>(eventNameBefore, commandBefore);
            _microsoftSqlClientDiagnostics.OnNext(kvBefore);
            
            const string eventNameAfter = MicrosoftSqlClientDiagnosticOptions.EventNames.WriteCommandAfter;
            var commandAfter = new {Command = new SqlCommand("Insert into"), Id = Guid.NewGuid()};
            var kvAfter = new KeyValuePair<string, object>(eventNameAfter, commandAfter);
            _microsoftSqlClientDiagnostics.OnNext(kvAfter);
        }
    }
}
