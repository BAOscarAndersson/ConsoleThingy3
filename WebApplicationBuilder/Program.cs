using Microsoft.Build.Execution;

BuildManager m = new();

ProjectInstance pi = new("../../WebApplication1/WebApplication1.csproj");

BuildRequestData d = new(pi, ["WebApplication1"]);

var r = m.BuildRequest(d);

Console.WriteLine(r.ResultsByTarget["WebApplication1"]);