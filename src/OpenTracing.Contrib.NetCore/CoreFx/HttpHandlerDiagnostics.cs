using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Contrib.NetCore.CoreFx
{
    /// <summary>
    /// Instruments outgoing HTTP calls that use <see cref="HttpClientHandler"/>.
    /// <para/>See https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandler.cs
    /// <para/>and https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/DiagnosticsHandlerLoggingStrings.cs
    /// </summary>
    internal sealed class HttpHandlerDiagnostics : DiagnosticListenerObserver
    {
        public const string DiagnosticListenerName = "HttpHandlerDiagnosticListener";

        private const string PropertiesKey = "ot-Span";

        private static readonly PropertyFetcher _activityStart_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _activityStop_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _activityStop_ResponseFetcher = new PropertyFetcher("Response");
        private static readonly PropertyFetcher _activityStop_RequestTaskStatusFetcher = new PropertyFetcher("RequestTaskStatus");
        private static readonly PropertyFetcher _exception_RequestFetcher = new PropertyFetcher("Request");
        private static readonly PropertyFetcher _exception_ExceptionFetcher = new PropertyFetcher("Exception");

        private readonly HttpHandlerDiagnosticOptions _options;

        protected override string GetListenerName() => DiagnosticListenerName;

        public HttpHandlerDiagnostics(ILoggerFactory loggerFactory, ITracer tracer, IOptions<HttpHandlerDiagnosticOptions> options)
            : base(loggerFactory, tracer)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override void OnNext(string eventName, object untypedArg)
        {
            switch (eventName)
            {
                case "System.Net.Http.HttpRequestOut.Start":
                    {
                        var request = (HttpRequestMessage)_activityStart_RequestFetcher.Fetch(untypedArg);

                        if (IgnoreRequest(request))
                        {
                            Logger.LogDebug("Ignoring Request {RequestUri}", request.RequestUri);
                            return;
                        }

                        string operationName = _options.OperationNameResolver(request);

                        var scope = Tracer.BuildSpan(operationName)
                            .WithTag(Tags.SpanKind.Key, Tags.SpanKindClient)
                            .WithTag(Tags.Component.Key, _options.ComponentName)
                            .WithTag(Tags.HttpMethod.Key, request.Method.ToString())
                            .WithTag(Tags.HttpUrl.Key, request.RequestUri.ToString())
                            .WithTag(Tags.PeerHostname.Key, request.RequestUri.Host)
                            .WithTag(Tags.PeerPort.Key, request.RequestUri.Port)
                            .StartActive(false);

                        _options.OnRequest?.Invoke(scope.Span, request);

                        if (_options.InjectEnabled?.Invoke(request) ?? true)
                        {
                            Tracer.Inject(scope.Span.Context, BuiltinFormats.HttpHeaders, new HttpHeadersInjectAdapter(request.Headers));
                        }

                        // This throws if there's already an item with the same key. We do this for now to get notified of potential bugs.
                        request.Properties.Add(PropertiesKey, scope.Span);
                    }
                    break;

                case "System.Net.Http.Exception":
                    {
                        var request = (HttpRequestMessage)_exception_RequestFetcher.Fetch(untypedArg);

                        if (request.Properties.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                        {
                            var exception = (Exception)_exception_ExceptionFetcher.Fetch(untypedArg);

                            span.SetException(exception);
                        }
                    }
                    break;

                case "System.Net.Http.HttpRequestOut.Stop":
                    {
                        var request = (HttpRequestMessage)_activityStop_RequestFetcher.Fetch(untypedArg);

                        if (request.Properties.TryGetValue(PropertiesKey, out object objSpan) && objSpan is ISpan span)
                        {
                            var response = (HttpResponseMessage)_activityStop_ResponseFetcher.Fetch(untypedArg);
                            var requestTaskStatus = (TaskStatus)_activityStop_RequestTaskStatusFetcher.Fetch(untypedArg);

                            if (response != null)
                            {
                                span.SetTag(Tags.HttpStatus.Key, (int)response.StatusCode);
                            }

                            if (requestTaskStatus == TaskStatus.Canceled || requestTaskStatus == TaskStatus.Faulted)
                            {
                                span.SetTag(Tags.Error.Key, true);
                            }

                            span.Finish();

                            request.Properties[PropertiesKey] = null;
                        }
                    }
                    break;
            }
        }

        private bool IgnoreRequest(HttpRequestMessage request)
        {
            foreach (Func<HttpRequestMessage, bool> ignore in _options.IgnorePatterns)
            {
                if (ignore(request))
                    return true;
            }

            return false;
        }
    }
}
