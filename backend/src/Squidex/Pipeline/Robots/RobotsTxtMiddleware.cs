﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;

namespace Squidex.Pipeline.Robots;

public sealed class RobotsTxtMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOptions<RobotsTxtOptions> robotsTxtOptions)
    {
        var text = robotsTxtOptions.Value.Text;

        if (CanServeRequest(context.Request) && !string.IsNullOrWhiteSpace(text))
        {
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = 200;

            await context.Response.WriteAsync(text, context.RequestAborted);
        }
        else
        {
            await next(context);
        }
    }

    private static bool CanServeRequest(HttpRequest request)
    {
        return HttpMethods.IsGet(request.Method) && string.IsNullOrEmpty(request.Path);
    }
}
