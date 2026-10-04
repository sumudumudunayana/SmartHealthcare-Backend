using Microsoft.EntityFrameworkCore;
using SmartHealthcare.API.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartHealthcare.API.Services;
using Microsoft.OpenApi;
using SmartHealthcare.API.AI.LLM;
using SmartHealthcare.API.AI.Orchestration;
using SmartHealthcare.API.AI.Agents;
using SmartHealthcare.API.AI.Tools;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// Render Port Configuration
// =========================================================

var port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}


// =========================================================
// Database Configuration
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Database connection string 'DefaultConnection' was not found."
    );
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// =========================================================
// JWT Configuration
// =========================================================

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT key 'Jwt:Key' was not found."
    );
}

builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme
)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                ),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ClockSkew = TimeSpan.Zero
        };
});

builder.Services.AddAuthorization();


// =========================================================
// Application Services
// =========================================================

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SpecializationService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<DoctorScheduleService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<PrescriptionService>();
builder.Services.AddScoped<LabReportService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<InsuranceClaimService>();
builder.Services.AddScoped<InsurancePolicyService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReceptionistService>();


// =========================================================
// AI Services
// =========================================================

builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<IAIOrchestrator, AIOrchestrator>();

builder.Services.AddScoped<IHealthcareAgent, AppointmentSchedulingAgent>();
builder.Services.AddScoped<IAppointmentSchedulingTool, AppointmentSchedulingTool>();

builder.Services.AddScoped<IAIWorkflowService, AIWorkflowService>();
builder.Services.AddScoped<IAIApprovalService, AIApprovalService>();

builder.Services.AddScoped<IHealthcareAgent, PatientTriageAgent>();
builder.Services.AddScoped<IHealthcareAgent, MedicalSummaryAgent>();
builder.Services.AddScoped<IMedicalSummaryTool, MedicalSummaryTool>();

builder.Services.AddScoped<IHealthcareAgent, BillingValidationAgent>();
builder.Services.AddScoped<IBillingValidationTool, BillingValidationTool>();

builder.Services.AddScoped<IAIRecommendationService, AIRecommendationService>();


// =========================================================
// CORS Configuration
// =========================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("SmartHealthcareFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// =========================================================
// Controllers & Swagger
// =========================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token."
        });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference(
                    "Bearer",
                    document
                )
            ] = []
        });
});


// =========================================================
// Forwarded Headers - Render Proxy
// =========================================================

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});


var app = builder.Build();


// =========================================================
// HTTP Request Pipeline
// =========================================================

app.UseForwardedHeaders();


// =========================================================
// Swagger
// =========================================================

app.UseSwagger();
app.UseSwaggerUI();


// =========================================================
// HTTPS
// =========================================================

// Render handles HTTPS at the proxy level.
// Do not redirect HTTPS inside the container.

// app.UseHttpsRedirection();


// =========================================================
// CORS
// =========================================================

// IMPORTANT:
// CORS must be before Authentication/Authorization.

app.UseCors("SmartHealthcareFrontend");


// =========================================================
// Authentication & Authorization
// =========================================================

app.UseAuthentication();

app.UseAuthorization();


// =========================================================
// Controllers
// =========================================================

app.MapControllers();


// =========================================================
// Run
// =========================================================

app.Run();