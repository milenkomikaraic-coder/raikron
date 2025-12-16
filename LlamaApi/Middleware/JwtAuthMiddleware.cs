using LlamaApi.Services.Auth;

namespace LlamaApi.Middleware;

public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;

    public JwtAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, JwtAuthService jwtService)
    {
        var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        if (!jwtService.ValidateToken(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = new { code = "unauthorized", message = "Invalid or missing token" } });
            return;
        }

        await _next(context);
    }
}
