using Bank.DB.Constants;
using Bank.Web.Infrastructure;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Bank.Tests.Web;

public class MustChangePasswordMiddlewareTests
{
    private static DefaultHttpContext BuildContext(string path, bool mustChange)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        if (mustChange)
        {
            claims.Add(new Claim(ClaimNames.MustChangePassword, "true"));
        }
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task Blocks_BusinessEndpoint_WhenMustChangePassword()
    {
        var context = BuildContext("/api/customers", mustChange: true);
        var nextCalled = false;
        var middleware = new MustChangePasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        using var _ = new AssertionScope();
        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Allows_ChangePasswordEndpoint_WhenMustChangePassword()
    {
        var context = BuildContext("/api/auth/change-password", mustChange: true);
        var nextCalled = false;
        var middleware = new MustChangePasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        using var _ = new AssertionScope();
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Allows_AllEndpoints_WhenFlagNotSet()
    {
        var context = BuildContext("/api/customers", mustChange: false);
        var nextCalled = false;
        var middleware = new MustChangePasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }
}
