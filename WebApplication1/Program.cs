var app = WebApplication.CreateBuilder(args).Build();

app.MapGet("hello", () => "world");

app.Run();
