using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using System.Reflection;
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

// Create a dictionary for fast assembly lookup
Dictionary<string, string> assemblyPaths = mrefs
    .OfType<PortableExecutableReference>()
    .Where(r => !string.IsNullOrEmpty(r.FilePath))
    .GroupBy(r => Path.GetFileNameWithoutExtension(r.FilePath))
    .ToDictionary(g => g.Key!, g => g.First().FilePath ?? "");

var assemblyContext = new AssemblyLoadContext("Idk");
assemblyContext.Resolving += Resolver;

try
{
    var l = assemblyContext.LoadFromStream(ms);
    var t = l.EntryPoint!;
    object[] margs = new object[1];
    string[] ma = [""];
    margs[0] = ma;
    var main = t.Invoke(null, margs);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}
Console.WriteLine("Done, press any key to continue.");
Console.ReadKey();

Assembly? Resolver(AssemblyLoadContext context, AssemblyName assemblyName)
{
    try
    {
        if (assemblyPaths.TryGetValue(assemblyName.Name!, out var path))
            return LoadAssembly(context, path);
        else
            return context.LoadFromAssemblyName(assemblyName);
    }
    catch
    {
        Console.WriteLine($"It is all hecked!");
        return null;
    }
}

static Assembly LoadAssembly(AssemblyLoadContext context, string path)
{
    // Convert reference assembly path to runtime assembly path
    var runtimePath = path
        .Replace(@"\packs\Microsoft.AspNetCore.App.Ref\",
                 @"\shared\Microsoft.AspNetCore.App\")
        .Replace(@"\packs\Microsoft.NETCore.App.Ref\",
                 @"\shared\Microsoft.NETCore.App\")
        .Replace(@"\ref\net9.0\",
                 @"\");

    return context.LoadFromAssemblyPath(GetPath(path, runtimePath));
}

static string GetPath(string path, string runtimePath)
{
    if (File.Exists(runtimePath))
        return runtimePath;
    else
        return path;
}