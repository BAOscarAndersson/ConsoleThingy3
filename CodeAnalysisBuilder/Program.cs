using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System.Runtime.Loader;

MSBuildLocator.RegisterDefaults();

using MSBuildWorkspace projectfiles = MSBuildWorkspace.Create();

Project project = await projectfiles
    .OpenProjectAsync(@"..\..\..\..\WebApplication1\WebApplication1.csproj");

Compilation? compilation = await project.GetCompilationAsync();

if (compilation is null) throw new Exception();

var diagnostics = compilation.GetDiagnostics();

foreach (Diagnostic diagnostic in diagnostics)
    Console.WriteLine(diagnostic);

var mrefs = compilation.References.ToArray();

IEnumerable<SyntaxTree> syntaxTrees = project
    .Documents
    .Select(x =>
    {
        _ = x.TryGetSyntaxTree(out var t);

        return t;
    })
    .Where(x => x is not null)
    .Select(x => x!);

using MemoryStream ms = new();

var compiled = CSharpCompilation
    .Create("IdkAgain", syntaxTrees, mrefs);

var r = compiled.Emit(ms);

if (!r.Success)
    throw new Exception("Failed to emit.");

ms.Seek(0, SeekOrigin.Begin);

var assemblyContext = new AssemblyLoadContext("Idk");
var l = assemblyContext.LoadFromStream(ms);
var t = l.EntryPoint;
object[] margs = new object[1];
string[] ma = [""];
margs[0] = ma;
try
{
    var main = t.Invoke(null, margs);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
Console.WriteLine("Done, press any key to continue.");
Console.ReadKey();