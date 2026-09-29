// <usings>
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Extensions;
using GettingStarted.Services;
using Serilog;
// </usings>

var builder = WebApplication.CreateBuilder(args);

// <logging>
// Serilog writes the log events; the Arc4u logger adds Category, SourceContext, Method and Application to each one.
builder.Services.AddSerilog(logger => logger
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Category,-10} {SourceContext}: {Message:lj}{NewLine}{Exception}"));
builder.Services.AddILogger();
// </logging>

// <configuration>
// Binds the Application.Configuration section to IOptions<ApplicationConfig>.
builder.Services.AddApplicationConfig(builder.Configuration);
// </configuration>

// <dependency-injection>
// Generated at compile time by Arc4u.Dependency.Tool from the [Export] attributes of this assembly.
builder.Services.RegisterGettingStartedTypes();
// </dependency-injection>

// <authentication>
// Validates JWT bearer tokens issued by the authority in the Authentication section.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
// </authentication>

// <pipeline>
// Errors without a body (404, unhandled exceptions, ...) become ProblemDetails responses.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
// </pipeline>

// <endpoints>
app.MapGet("/hello/{name}", (string name, IGreetingService greetings) => greetings.Greet(name));

app.MapGet("/me", (ClaimsPrincipal user) => user.Claims.Select(claim => new { claim.Type, claim.Value }))
   .RequireAuthorization();
// </endpoints>

app.Run();
