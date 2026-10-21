using Arc4u.Dependency;

// Builds the host with the Arc4u logger, then exits: the publish is the real test, this only checks that the native binary starts.
var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddILogger();

using var app = builder.Build();
Console.WriteLine("Arc4u Native AOT publish test: OK");
