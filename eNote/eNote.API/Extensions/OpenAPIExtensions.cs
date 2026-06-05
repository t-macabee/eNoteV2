using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace eNote.API.Extensions
{
    public static class OpenAPIExtensions
    {
        public static IServiceCollection AddScalarDocumentation(this IServiceCollection services)
        {
            services.AddOpenApi();
            return services;
        }

        public static IApplicationBuilder UseOpenApiPatcher(this IApplicationBuilder app)
        {
            return app.UseMiddleware<OpenApiPatcherMiddleware>();
        }
    }

    internal sealed class OpenApiPatcherMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            if (!HttpMethods.IsGet(context.Request.Method) || !context.Request.Path.Value?.Contains("openapi", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _next(context);
                return;
            }

            var originalBody = context.Response.Body;

            await using var mem = new MemoryStream();

            context.Response.Body = mem;

            await _next(context);

            mem.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(mem);

            var body = await reader.ReadToEndAsync();

            context.Response.Body = originalBody;

            if (string.IsNullOrWhiteSpace(body) || !body.Contains("\"openapi\"", StringComparison.OrdinalIgnoreCase))
            {
                await context.Response.WriteAsync(body);
                return;
            }

            try
            {
                var node = JsonNode.Parse(body) as JsonObject ?? [];
                var components = node["components"] as JsonObject ?? [];
                var securitySchemes = components["securitySchemes"] as JsonObject ?? [];

                if (!securitySchemes.ContainsKey("Bearer"))
                {
                    securitySchemes["Bearer"] = JsonNode.Parse(JsonSerializer.Serialize(new
                    {
                        type = "http",
                        scheme = "bearer",
                        bearerFormat = "JWT",
                        description = "Enter your valid JSON Web Token (JWT) here."
                    }));
                }

                components["securitySchemes"] = securitySchemes;
                node["components"] = components;

                if (node["paths"] is JsonObject paths)
                {
                    foreach (var kv in paths)
                    {
                        if (kv.Value is JsonObject pathObj)
                        {
                            foreach (var methodKv in pathObj)
                            {
                                if (methodKv.Value is JsonObject operationObj)
                                {
                                    var security = operationObj["security"] as JsonArray ?? [];
                                    var req = new JsonObject
                                    {
                                        ["Bearer"] = new JsonArray()
                                    };
                                    security.Add(req);
                                    operationObj["security"] = security;
                                }
                            }
                        }
                    }
                }

                var output = JsonSerializer.Serialize(node, options: new JsonSerializerOptions { WriteIndented = true });
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(output);
                await context.Response.WriteAsync(output);
            }
            catch
            {
                await context.Response.WriteAsync(body);
            }
        }
    }
}
