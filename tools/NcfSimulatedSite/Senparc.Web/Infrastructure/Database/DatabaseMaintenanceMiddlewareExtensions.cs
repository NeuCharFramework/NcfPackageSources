/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseMaintenanceMiddlewareExtensions.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.35.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

namespace Senparc.Web.Infrastructure.Database;

public static class DatabaseMaintenanceMiddlewareExtensions
{
    public const string MaintenancePath = "/Maintenance/DatabaseUpgrade";

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
