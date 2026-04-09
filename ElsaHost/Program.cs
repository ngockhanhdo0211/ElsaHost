using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Parameters;
using ElsaHost;
using ElsaHost.Data;
using ElsaHost.Models;
using ElsaHost.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
var builder = WebApplication.CreateBuilder(args);

// Connection string dùng chung cho Elsa + dữ liệu nghiệp vụ seminar.
var connectionString = builder.Configuration.GetConnectionString("ElsaSqlServer")
                      ?? throw new InvalidOperationException("Connection string 'ElsaSqlServer' not found.");

// DbContext nghiệp vụ riêng của ứng dụng.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Service nghiệp vụ.
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ElsaHost API",
        Version = "v1",
        Description = "API cho seminar Elsa Workflow"
    });

    options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", "."));
});

builder.Services.AddElsa(elsa =>
{
    // Management persistence
    elsa.UseWorkflowManagement(management =>
        management.UseEntityFrameworkCore(ef => ef.UseSqlServer(connectionString)));

    // Runtime persistence
    elsa.UseWorkflowRuntime(runtime =>
        runtime.UseEntityFrameworkCore(ef => ef.UseSqlServer(connectionString)));

    // Identity
    elsa.UseIdentity(identity =>
    {
       identity.TokenOptions = options =>
            options.SigningKey = "this-is-a-very-long-development-signing-key-1234567890";
      identity.UseAdminUserProvider();
    });

    // Authentication
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    // Elsa API
    elsa.UseWorkflowsApi();

    // SignalR realtime updates
    elsa.UseRealTimeWorkflows();

    // HTTP activities
    elsa.UseHttp(http => http.ConfigureHttpOptions = options =>
        options.BaseUrl = new Uri("https://localhost:7045"));

    // Scheduling
    elsa.UseScheduling();

    // Quét custom activities / workflows trong project ElsaHost
    elsa.AddActivitiesFrom<Program>();
    elsa.AddWorkflowsFrom<Program>();
});

// CORS cho ElsaStudio
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .WithOrigins("https://localhost:7263", "https://localhost:7045", "http://localhost:5142")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithExposedHeaders("x-elsa-workflow-instance-id")));

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();
app.UseRouting();
// Tạm comment khi test local
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseSwagger(options =>
{
    options.RouteTemplate = "swagger/{documentName}/swagger.json";
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ElsaHost API v1");
    options.RoutePrefix = "swagger";
});

// Elsa middleware
app.UseWorkflowsApi();
app.UseWorkflows();
app.UseWorkflowsSignalRHubs();

// Test routes
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Text("ElsaHost is running"));

// Create leave request + start workflow
app.MapPost("/api/leave-requests", async (
    CreateLeaveRequestDto dto,
    ILeaveRequestService leaveRequestService,
    AppDbContext dbContext,
    IWorkflowRuntime workflowRuntime,
    CancellationToken cancellationToken) =>
{
    try
    {
        var leaveRequest = await leaveRequestService.CreateAsync(dto, cancellationToken);

        var workflowResult = await workflowRuntime.StartWorkflowAsync(
            nameof(LeaveApprovalWorkflow),
            new StartWorkflowRuntimeParams
            {
                Input = new Dictionary<string, object>
                {
                    ["LeaveRequestId"] = leaveRequest.Id,
                    ["TotalDays"] = leaveRequest.TotalDays
                },
                CorrelationId = $"leave-request-{leaveRequest.Id}"
            });

        leaveRequest.WorkflowInstanceId = workflowResult.WorkflowInstanceId;
        leaveRequest.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            message = "Leave request created successfully",
            data = leaveRequest,
            workflowInstanceId = workflowResult.WorkflowInstanceId
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

// Get all leave requests
app.MapGet("/api/leave-requests", async (
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    var requests = await leaveRequestService.GetAllAsync(cancellationToken);
    return Results.Ok(requests);
});

// Get single leave request
app.MapGet("/api/leave-requests/{id:int}", async (
    int id,
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    var request = await leaveRequestService.GetByIdAsync(id, cancellationToken);
    return request is null ? Results.NotFound() : Results.Ok(request);
});

// Get leave request history
app.MapGet("/api/leave-requests/{id:int}/history", async (
    int id,
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    var history = await leaveRequestService.GetHistoryAsync(id, cancellationToken);
    return Results.Ok(history);
});
// Internal API: update leave request status/current step
app.MapPost("/api/leave-requests/internal/update-status", async (
    UpdateLeaveRequestStatusDto dto,
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var updatedRequest = await leaveRequestService.UpdateStatusAsync(dto, cancellationToken);

        return Results.Ok(new
        {
            message = "Leave request status updated successfully",
            data = updatedRequest
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
})
.AllowAnonymous();

// Manager decision
app.MapPost("/api/leave-requests/{id:int}/manager-decision", async (
    int id,
    DecisionDto dto,
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    var result = await leaveRequestService.ManagerDecisionAsync(id, dto, cancellationToken);

    if (!result.Success)
        return Results.BadRequest(new { message = result.Message });

    var request = await leaveRequestService.GetByIdAsync(id, cancellationToken);

    return Results.Ok(new
    {
        message = result.Message,
        data = request
    });
});

// HR decision
app.MapPost("/api/leave-requests/{id:int}/hr-decision", async (
    int id,
    DecisionDto dto,
    ILeaveRequestService leaveRequestService,
    CancellationToken cancellationToken) =>
{
    var result = await leaveRequestService.HrDecisionAsync(id, dto, cancellationToken);

    if (!result.Success)
        return Results.BadRequest(new { message = result.Message });

    var request = await leaveRequestService.GetByIdAsync(id, cancellationToken);

    return Results.Ok(new
    {
        message = result.Message,
        data = request
    });
});

app.Run();