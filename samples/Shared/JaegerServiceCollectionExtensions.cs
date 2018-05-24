﻿using System;
using System.Reflection;
using Jaeger.Core;
using Jaeger.Core.Metrics;
using Jaeger.Core.Reporters;
using Jaeger.Core.Samplers;
using Jaeger.Transport.Thrift.Transport;
using Microsoft.Extensions.Logging;
using OpenTracing;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Util;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JaegerServiceCollectionExtensions
    {
        private static readonly Uri _jaegerUri = new Uri("http://localhost:14268/api/traces");

        public static IServiceCollection AddJaeger(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // TODO !!!!
            // services.AddSingleton<ITracer>(serviceProvider =>
            // {
            //     string serviceName = Assembly.GetEntryAssembly().GetName().Name;

            //     ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            //     ISampler sampler = new ConstSampler(sample: true);

            //     IReporter reporter = new RemoteReporter.Builder(new JaegerHttpTransport(_jaegerUri, batchSize: 3))
            //         .WithMetricsFactory(NoopMetricsFactory.Instance)
            //         .WithLoggerFactory(loggerFactory)
            //         .Build();

            //     ITracer tracer = new Tracer.Builder(serviceName)
            //         .WithLoggerFactory(loggerFactory)
            //         .WithSampler(sampler)
            //         .WithReporter(reporter)
            //         .Build();

            //     GlobalTracer.Register(tracer);

            //     return tracer;
            // });

            // Prevent endless loops when OpenTracing is tracking HTTP requests to Jaeger.
            services.Configure<HttpHandlerDiagnosticOptions>(options =>
            {
                options.IgnorePatterns.Add(request => _jaegerUri.IsBaseOf(request.RequestUri));
            });

            return services;
        }
    }
}
