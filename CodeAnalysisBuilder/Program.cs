using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;

const string baseAddress = "http://localhost:666/";

string projectPath = @"..\..\..\..\WebApplication1\WebApplication1.csproj";

MSBuildLocator.RegisterDefaults();

using MSBuildWorkspace projectfiles = MSBuildWorkspace.Create();

Project project = await projectfiles
    .OpenProjectAsync(projectPath);

Compilation compilation = await project.GetCompilationAsync() 
    ?? throw new Exception("Did not compile");

ImmutableArray<Diagnostic> diagnostics = compilation
    .GetDiagnostics();

if (diagnostics.Any(x => x.Severity == DiagnosticSeverity.Error))
    throw new Exception("Could not compile.");

MetadataReference[] mrefs = compilation.References.ToArray();

using MemoryStream ms = new();

EmitResult r = compilation.Emit(ms);

if (!r.Success) throw new Exception("Failed to emit.");

ms.Seek(0, SeekOrigin.Begin);

/* Manually convert reference assemblies to implementation assemblies.
   Compilation needs to be done against the reference so we need to
   change them during assembly resolution.*/
Dictionary<string, string> implementingAssemblies = mrefs
    .OfType<PortableExecutableReference>()
    .Where(r => !string.IsNullOrEmpty(r.FilePath))
    .Select(ReferenceWithImplementationPath)
    .ToDictionary();

AssemblyLoadContext assemblyContext = new("Idk");

assemblyContext.Resolving += Resolver;

try
{
    _ = Task.Run(() =>
    assemblyContext
        .LoadFromStream(ms)
        .EntryPoint!
        .Invoke(null, WithUrls()));

    HttpClient client = new()
    {
        BaseAddress = new Uri(baseAddress)
    };

    await Task.Delay(1555); // Make sure the server has started.

    HttpResponseMessage response = await client.GetAsync("hello");

    response.EnsureSuccessStatusCode();

    string content = await response.Content.ReadAsStringAsync();

    Console.WriteLine(content);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException is not null)
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
}

Console.WriteLine("Done, press any key to continue.");
Console.ReadKey();

Assembly? Resolver(AssemblyLoadContext context, AssemblyName assemblyName)
{
    if (implementingAssemblies.TryGetValue(assemblyName.Name!, out string? path))
        return context.LoadFromAssemblyPath(path);
    else
        return null;
}

static (string, string) ReferenceWithImplementationPath(
    PortableExecutableReference reference)
{
    string name = Path
        .GetFileNameWithoutExtension(reference.FilePath) ?? "";

    string implementationPath = ImplementationPath(reference.FilePath ?? "");

    return (name, implementationPath);
}

static string ImplementationPath(string path)
{
    // Convert reference assembly path to runtime assembly path
    string runtimePath = path
        .Replace(@"\packs\Microsoft.AspNetCore.App.Ref\",
                 @"\shared\Microsoft.AspNetCore.App\")
        .Replace(@"\packs\Microsoft.NETCore.App.Ref\",
                 @"\shared\Microsoft.NETCore.App\")
        .Replace(@"\ref\net9.0\",
                 @"\");

    if (File.Exists(runtimePath))
        return runtimePath;
    else
        return path;
}

static object[] WithUrls()
{
    object[] margs = new object[1];
    string[] ma = [$"--urls={baseAddress}"];
    margs[0] = ma;
    return margs;
}