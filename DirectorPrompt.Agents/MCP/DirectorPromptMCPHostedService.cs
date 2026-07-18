using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using Serilog;

namespace DirectorPrompt.Agents.MCP;

public sealed class DirectorPromptMCPHostedService
(
    MCPProjectTools projectTools
) : IHostedService, IAsyncDisposable, IDirectorPromptMCPStatus
{
    private const string ENDPOINT = "http://127.0.0.1:33145/mcp";

    private const string RATE_LIMITER_POLICY = "mcp";

    private WebApplication? application;

    public string Endpoint => ENDPOINT;

    public bool IsAvailable { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseSetting("urls", "http://127.0.0.1:33145");
        builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(3));
        builder.Services.AddRateLimiter
        (options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, _) =>
                {
                    Log.Warning
                    (
                        "内部 MCP 请求已被限流: {Method} {Path}",
                        context.HttpContext.Request.Method,
                        context.HttpContext.Request.Path
                    );
                    return ValueTask.CompletedTask;
                };
                options.AddConcurrencyLimiter
                (
                    RATE_LIMITER_POLICY,
                    limiterOptions =>
                    {
                        limiterOptions.PermitLimit          = 1;
                        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        limiterOptions.QueueLimit           = 16;
                    }
                );
            }
        );
        builder.Services.AddMcpServer
               (options => options.ServerInfo = new Implementation
                   {
                       Name    = "director-prompt",
                       Version = "2"
                   }
               )
               .WithHttpTransport()
               .WithTools(projectTools)
               .WithMessageFilters
               (filters => filters.AddIncomingFilter
                (next => async (context, cancellationToken) =>
                    {
                        if (context.JsonRpcMessage is JsonRpcRequest
                            {
                                Method: RequestMethods.ToolsCall,
                                Params: not null
                            } request)
                        {
                            var toolName  = request.Params["name"]?.GetValue<string>();
                            var arguments = request.Params["arguments"]?.ToJsonString();

                            Log.Information
                            (
                                "内部 MCP 工具请求: {ToolName}, 参数={Arguments}",
                                toolName,
                                arguments
                            );
                        }

                        await next(context, cancellationToken);
                    }
                )
               );

        var created = builder.Build();
        created.UseRateLimiter();
        created.MapMcp("/mcp").RequireRateLimiting(RATE_LIMITER_POLICY);

        try
        {
            await created.StartAsync(cancellationToken);
            application  = created;
            IsAvailable  = true;
            ErrorMessage = null;
            Log.Information("内部 MCP 服务已启动: {Endpoint}", ENDPOINT);
        }
        catch (Exception exception)
        {
            IsAvailable  = false;
            ErrorMessage = exception.Message;
            Log.Warning(exception, "内部 MCP 服务启动失败: {Endpoint}", ENDPOINT);
            await DisposeApplicationAsync(created);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var currentApplication = application;
        application = null;
        IsAvailable = false;

        if (currentApplication is null)
            return;

        await StopApplicationAsync(currentApplication, cancellationToken);
    }

    public async ValueTask DisposeAsync() =>
        await StopAsync(CancellationToken.None);

    private static async Task StopApplicationAsync(WebApplication application, CancellationToken cancellationToken)
    {
        using var stopTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stopTimeout.CancelAfter(TimeSpan.FromSeconds(3));

        try
        {
            await application.StopAsync(stopTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stopTimeout.IsCancellationRequested)
        {
            Log.Warning("内部 MCP 服务停止超时, 将继续退出应用");
        }

        await DisposeApplicationAsync(application);
    }

    private static async Task DisposeApplicationAsync(WebApplication application)
    {
        try
        {
            await application.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Log.Warning("内部 MCP 服务释放超时, 将继续退出应用");
        }
    }
}
