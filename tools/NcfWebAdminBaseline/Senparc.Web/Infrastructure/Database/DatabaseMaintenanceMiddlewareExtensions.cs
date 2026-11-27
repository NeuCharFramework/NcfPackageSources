/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseMaintenanceMiddlewareExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.39.10 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public static class DatabaseMaintenanceMiddlewareExtensions
{
    public const string MaintenancePath = "/Maintenance/DatabaseUpgrade";

    /// <summary>
    /// Adds maintenance-mode routing that allows health and maintenance endpoints,
    /// redirects HTML requests when the database is unavailable, and returns a
    /// structured 503 response for other requests.
    /// </summary>
    /// <param name="app">The application builder receiving the middleware.</param>
    /// <returns>The same application builder for fluent configuration.</returns>
    public static IApplicationBuilder UseDatabaseMaintenanceMode(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var state = context.RequestServices.GetRequiredService<DatabaseRuntimeStateStore>().Current;
            if (state.Status is DatabaseRuntimeStatus.Ready or DatabaseRuntimeStatus.Uninitialized
                || context.Request.Path.StartsWithSegments(MaintenancePath)
                //Legacy comment translated to English.
                || context.Request.Path.StartsWithSegments("/alive"))
            {
                await next().ConfigureAwait(false);
                return;
            }

            var acceptsHtml = context.Request.Headers.Accept.Any(value =>
                value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
            if (HttpMethods.IsGet(context.Request.Method) && acceptsHtml)
            {
                context.Response.Redirect(MaintenancePath);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "database_not_ready",
                status = state.Status.ToString(),
                message = state.Message,
                maintenanceUrl = MaintenancePath
            }).ConfigureAwait(false);
        });
    }
}
