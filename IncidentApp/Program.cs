using IncidentApp.Data;
using IncidentApp.Repositories;
using IncidentApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IncidentApp.Middleware;
using Microsoft.OpenApi.Models;
using IncidentApp.AI;
using IncidentApp.AI.Validation;
using IncidentApp.AI.Mapping;
using IncidentApp.AI.Embedding;
using IncidentApp.AI.VectorSearch;
using IncidentApp.AI.FunctionCalling;
using IncidentApp.AI.Resilience;
using IncidentApp.AI.Evaluation;
using IncidentApp.AI.Agents;
using IncidentApp.AI.SemanticKernel;
using IncidentApp.AI.MCP;
using IncidentApp.AI.Security;

var builder = WebApplication.CreateBuilder(args);

#region DB + DI
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IncidentService>();
builder.Services.AddScoped<AIOrchestrationService>();
builder.Services.AddScoped<AIGovernanceService>();

builder.Services.AddScoped<SemanticKernelService>();
builder.Services.AddScoped<SemanticKernelEmbeddingService>();
builder.Services.AddScoped<AIResponseValidator>();
builder.Services.AddScoped<AIResponseMapper>();
builder.Services.AddScoped<QdrantVectorSearchService>();
builder.Services.AddScoped<GroqService>();

// AI-Enabled Features
builder.Services.AddScoped<IncidentTools>();
builder.Services.AddSingleton<PollyResilienceService>();
builder.Services.AddSingleton<AIEvaluationService>();
builder.Services.AddScoped<AgenticWorkflowService>();

// MCP Features
builder.Services.AddSingleton<MCPServer>();
builder.Services.AddScoped<MCPToolAdapter>();

// AI Security Features
builder.Services.AddSingleton<PromptInjectionDetector>();
builder.Services.AddSingleton<PIIRedactionService>();
builder.Services.AddSingleton<AIInputSanitizer>();
#endregion

// -------------------- CONTROLLERS --------------------
builder.Services.AddControllers();

// -------------------- SWAGGER --------------------
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Incident API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


// -------------------- JWT AUTH --------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });


// -------------------- CORS (ADDED FIX) --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


// -------------------- BUILD APP --------------------
var app = builder.Build();


// -------------------- PIPELINE --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseRouting();

// ?? CORS MUST BE HERE (VERY IMPORTANT)
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
