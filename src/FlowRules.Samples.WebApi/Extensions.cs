using FlowRules.Engine;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FlowRules.Samples.WebApi;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();

            builder.AddDefaultHealthChecks();

            builder.Services.AddServiceDiscovery();

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                // Turn on resilience by default
                http.AddStandardResilienceHandler();

                // Turn on service discovery by default
                http.AddServiceDiscovery();
            });

            // Uncomment the following to restrict the allowed schemes for service discovery.
            // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
            // {
            //     options.AllowedSchemes = ["https"];
            // });

            return builder;
        }

        public TBuilder ConfigureOpenTelemetry()
        {
            string serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? builder.Environment.ApplicationName;
            string endpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4318";
            OtlpExportProtocol protocol = string.Equals(
                builder.Configuration["OTEL_EXPORTER_OTLP_PROTOCOL"], "grpc", StringComparison.OrdinalIgnoreCase)
                    ? OtlpExportProtocol.Grpc
                    : OtlpExportProtocol.HttpProtobuf;

            ResourceBuilder resource = ResourceBuilder.CreateDefault()
                .AddService(serviceName)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector();

            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.SetResourceBuilder(resource);
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri($"{endpoint}/v1/logs");
                    o.Protocol = protocol;
                });
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    metrics
                        .SetResourceBuilder(resource)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter(FlowRulesTelemetry.MeterName)
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri($"{endpoint}/v1/metrics");
                            o.Protocol = protocol;
                        });
                })
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resource)
                        .AddSource(builder.Environment.ApplicationName, FlowRulesTelemetry.ActivitySourceName)
                        .AddAspNetCoreInstrumentation(o =>
                            o.Filter = context =>
                                !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath))
                        .AddHttpClientInstrumentation()
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri($"{endpoint}/v1/traces");
                            o.Protocol = protocol;
                        });
                });

            return builder;
        }

        private TBuilder AddOpenTelemetryExporters()
        {
            // Exporters are now configured per-signal in ConfigureOpenTelemetry with explicit /v1/* paths.
            return builder;
        }

        public TBuilder AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                // Add a default liveness check to ensure app is responsive
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            return builder;
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}
