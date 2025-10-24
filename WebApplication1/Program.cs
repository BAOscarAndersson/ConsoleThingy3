var app = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args).Build();

app.MapGet("hello", () => "world");

app.Run();
