using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTracing.Contrib.NetCore.AspNetCore;
using OpenTracing.Contrib.NetCore.Configuration;
using OpenTracing.Contrib.NetCore.CoreFx;
using OpenTracing.Contrib.NetCore.EntityFrameworkCore;
using OpenTracing.Contrib.NetCore.Internal;
using OpenTracing.Contrib.NetCore.Logging;
using OpenTracing.Contrib.NetCore.MicrosoftSqlClient;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OpenTracingBuilderExtensions
    {
        internal static IOpenTracingBuilder AddDiagnosticSubscriber<TDiagnosticSubscriber>(this IOpenTracingBuilder builder)
            where TDiagnosticSubscriber : DiagnosticObserver
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<DiagnosticObserver, TDiagnosticSubscriber>());

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<AspNetCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(AspNetCoreDiagnostics.DiagnosticListenerName));

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for ASP.NET Core.
        /// </summary>
        public static IOpenTracingBuilder AddAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options)
        {
            AddAspNetCore(builder);
            return ConfigureAspNetCore(builder, options);
        }

        public static IOpenTracingBuilder ConfigureAspNetCore(this IOpenTracingBuilder builder, Action<AspNetCoreDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for the .NET framework BCL.
        /// </summary>
        public static IOpenTracingBuilder AddCoreFx(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<GenericDiagnostics>();

            builder.AddDiagnosticSubscriber<HttpHandlerDiagnostics>();
            builder.ConfigureGenericDiagnostics(options => options.IgnoredListenerNames.Add(HttpHandlerDiagnostics.DiagnosticListenerName));

            builder.AddDiagnosticSubscriber<SqlClientDiagnostics>();
            builder.ConfigureGenericDiagnostics(options => options.IgnoredListenerNames.Add(SqlClientDiagnostics.DiagnosticListenerName));

            return builder;
        }

        public static IOpenTracingBuilder ConfigureGenericDiagnostics(this IOpenTracingBuilder builder, Action<GenericDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        public static IOpenTracingBuilder ConfigureGenericEvents(this IOpenTracingBuilder builder, Action<GenericEventOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        public static IOpenTracingBuilder ConfigureSqlClientDiagnostics(this IOpenTracingBuilder builder, Action<SqlClientDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for Microsoft.Data.SqlClient.
        /// </summary>
        public static IOpenTracingBuilder AddMicrosoftSqlClient(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<MicrosoftSqlClientDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(MicrosoftSqlClientDiagnostics.DiagnosticListenerName));

            return builder;
        }

        /// <summary>
        /// Configuration options for the instrumentation of Microsoft.Data.SqlClient.
        /// </summary>
        public static IOpenTracingBuilder ConfigureMicrosoftSqlClient(this IOpenTracingBuilder builder, Action<MicrosoftSqlClientDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddDiagnosticSubscriber<EntityFrameworkCoreDiagnostics>();
            builder.ConfigureGenericDiagnostics(genericOptions => genericOptions.IgnoredListenerNames.Add(EntityFrameworkCoreDiagnostics.DiagnosticListenerName));

            return builder;
        }

        /// <summary>
        /// Adds instrumentation for Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder AddEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreDiagnosticOptions> options)
        {
            AddEntityFrameworkCore(builder);
            return ConfigureEntityFrameworkCore(builder, options);
        }

        /// <summary>
        /// Configuration options for the instrumentation of Entity Framework Core.
        /// </summary>
        public static IOpenTracingBuilder ConfigureEntityFrameworkCore(this IOpenTracingBuilder builder, Action<EntityFrameworkCoreDiagnosticOptions> options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (options != null)
            {
                builder.Services.Configure(options);
            }

            return builder;
        }

        public static IOpenTracingBuilder AddLoggerProvider(this IOpenTracingBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, OpenTracingLoggerProvider>());
            builder.Services.Configure<LoggerFilterOptions>(options =>
            {
                // All interesting request-specific logs are instrumented via DiagnosticSource.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.AspNetCore.Hosting", LogLevel.None);

                // EF Core is sending everything to DiagnosticSource AND ILogger so we completely disable the category.
                options.AddFilter<OpenTracingLoggerProvider>("Microsoft.EntityFrameworkCore", LogLevel.None);
            });

            return builder;
        }
    }
}
