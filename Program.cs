using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

// See https://aka.ms/new-console-template for more information
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.EntityComponents.Blocks;
using System.Data;
using System.Security.Cryptography.Xml;
using VRageMath;

internal class Program
{
    private static void Main(string[] args)
    {
        Log("Start");
        Test1();
    }

    static void Test1()
    {
        var modulename = "KeenFixProject";

        var solution = LoadSolutionFromScript(modulename);

        test(solution, modulename+".dll");

        CompileSolution(solution, null);
    }

    static void test(Solution solution, string module)
    {
        ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();
        foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
        {
            var project = solution.GetProject(projectId);

            Compilation projectCompilation = project.GetCompilationAsync().Result;

            var collect = CollectFromInterfaces(projectCompilation, module);
            GenerateWrappers(collect);
        }

    }



    static async Task<Solution> LoadSolution(string inputPath)
    {
        // Register MSBuild
        MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();

        // Load project or solution
        var project = inputPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            ? (await workspace.OpenSolutionAsync(inputPath)).Projects.First()
            : await workspace.OpenProjectAsync(inputPath);

        var compilation = await project.GetCompilationAsync();
        if (compilation == null)
        {
            Console.WriteLine("Failed to get project compilation.");
            return null;
        }
        return null;
    }

    static Solution LoadSolutionFromScript(string modulename)
    {
        var sourceCodeFilePath = @"C:\Users\thoma\source\repos\Program.OreScoutDrone\SpaceEngineers.TurretsTargetAcquistion\SEScriptBuilder\Merge.cs";
        var resultCodeDllPath = @"C:\Users\thoma\MergeFix.dll";
        var resultCodeFilePath = @"C:\Users\thoma\MergeFix.cs";

        var sourceCode = File.ReadAllText(sourceCodeFilePath);

        // Find where the MSBuild assemblies are located on your system.
        // If you need a specific version, you can use MSBuildLocator.RegisterMSBuildPath.
        Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults();

        // Note that you may need to restore the NuGet packages for the solution before opeing it with Roslyn.
        // Depending on what you want to do, dependencies may be required for a correct analysis.

        // Create a Roslyn workspace and load the solution
        //using (var workspace = MSBuildWorkspace.Create())
        using (var workspace = new AdhocWorkspace())
        {

            CSharpCompilationOptions DefaultCompilationOptions =
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOverflowChecks(true)
                    .WithOptimizationLevel(OptimizationLevel.Release);

            var projectId = ProjectId.CreateNewId();
            //var modulename = "KeenFixProject";
            var module = modulename + ".dll";

            var info = ProjectInfo.Create(
                projectId,
                version: VersionStamp.Default,
                name: "KeenFixProject",
                assemblyName: modulename,
                language: LanguageNames.CSharp,
                compilationOptions: DefaultCompilationOptions);

            var project = workspace.AddProject(info);

            var text = SourceText.From(sourceCode);

            var document = workspace.AddDocument(project.Id, "KeenFixProject.cs", text);


            List<MetadataReference> references = new List<MetadataReference>();
            //references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load(new System.Reflection.AssemblyName("netstandard")).Location));
            //references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load(new System.Reflection.AssemblyName("mscorlib")).Location));

            foreach (string dll in System.IO.Directory.GetFiles(@"C:\Program Files\dotnet\packs\NETStandard.Library.Ref\2.1.0\ref\netstandard2.1", "*.dll"))
            {
                if (!dll.Contains("VisualBasic"))
                    references.Add(MetadataReference.CreateFromFile(dll));
            }
            foreach (string dll in System.IO.Directory.GetFiles(@"C:\Users\thoma\.nuget\packages\spaceengineers.scriptingreferences\1.3.0\lib\net46", "*.dll"))
            {
                references.Add(MetadataReference.CreateFromFile(dll));
            }
            //foreach (string dll in System.IO.Directory.GetFiles(@"C:\Users\thoma\.nuget\packages\microsoft.csharp\4.7.0\lib\netstandard2.0", "*.dll"))
            //{
            //    references.Add(MetadataReference.CreateFromFile(dll));
            //}
            //C:\Users\thoma\.nuget\packages\microsoft.codeanalysis.csharp.workspaces\4.13.0\lib\netstandard2.0
            //C:\Users\thoma\.nuget\packages\microsoft.csharp\4.7.0\lib\netstandard2.0

            var solution = document.Project.Solution.AddMetadataReferences(projectId, references);

            var success = workspace.TryApplyChanges(solution);

            return solution;

        }
    }

    static IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamespaceOrTypeSymbol ns, string module)
    {
        foreach (var member in ns.GetMembers())
        {
            if (member.ContainingModule != null && member.ContainingModule.ToString().Equals(module))
            {
                if (member is INamespaceOrTypeSymbol nested)
                {
                    if (member is INamedTypeSymbol type && (type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class && type.IsAbstract))
                    {
                        //Console.WriteLine(member.Name + " : " + member.ContainingModule);
                        yield return type;
                    }
                    else
                    {
                        foreach (var sub in GetAllInterfaces(nested, module))
                            yield return sub;
                    }
                }

            }
        }
    }

    static Dictionary<IMethodSymbol, ITypeSymbol> CollectFromInterfaces(Compilation compilation, string module)
    {
        // Collect unique non-void return types from interfaces
        var returnTypes = new Dictionary<IMethodSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var iface in GetAllInterfaces(compilation.GlobalNamespace, module))
        {
            foreach (var method in iface.GetMembers().OfType<IMethodSymbol>())
            {
                if (method.MethodKind == MethodKind.Ordinary && !method.ReturnsVoid)
                {
                    var type = method.ReturnType as INamedTypeSymbol;
                    if (type != null && type.TypeArguments.Length > 0)
                        returnTypes.Add(method, method.ReturnType);
                }
            }
        }
        return returnTypes;
    }

    static void GenerateWrappers(Dictionary<IMethodSymbol, ITypeSymbol> returnTypes)
    {

        foreach (var kv in returnTypes)
        {
            Console.WriteLine(kv.Key.ReceiverType.ToString() + " : " + kv.Value.ToString());
        }

        // Generate one wrapper class per return type
        foreach (var type in returnTypes.Values)
        {


            // Fully qualified return type (preserves generic args)
            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                    .Replace("global::", string.Empty);
            var simpleName = type.Name + "Wrapper";

            // Usings
            var usings = new[] { "System" }
                .Concat(type.ContainingNamespace.IsGlobalNamespace ? Array.Empty<string>() : new[] { type.ContainingNamespace.ToDisplayString() })
                .Distinct()
                .Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))
                .ToArray();

            // class SimpleNameWrapper : OriginalReturnType { }
            var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeName));
            var classDecl = SyntaxFactory.ClassDeclaration(simpleName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)))
                .NormalizeWhitespace();

            // Compilation unit
            var compUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(usings)
                .AddMembers(
                    SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName("GeneratedWrappers"))
                        .AddMembers(classDecl))
                .NormalizeWhitespace();

            // Write file
            //var filePath = Path.Combine(outputDir, simpleName + ".cs");
            //File.WriteAllText(filePath, compUnit.ToFullString());
            //Console.WriteLine($"Written {filePath}");
            var code = compUnit.ToFullString();
        }
    }

    static bool CompileSolution(Solution Solution, string url = null)
    {
        bool success = true;

        ProjectDependencyGraph projectGraph = Solution.GetProjectDependencyGraph();
        Dictionary<string, Stream> assemblies = new Dictionary<string, Stream>();

        foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
        {
            var project = Solution.GetProject(projectId);

            Compilation projectCompilation = project.GetCompilationAsync().Result;
            if (null != projectCompilation && !string.IsNullOrEmpty(projectCompilation.AssemblyName))
            {
                using (var stream = new MemoryStream())
                {
                    EmitResult result = projectCompilation.Emit(stream);
                    success = result.Success;

                    foreach (var d in result.Diagnostics)
                    {
                        Log
                        (
                            d.Severity.ToString() +
                            " Position " + d.Location.GetLineSpan().StartLinePosition +
                            " : " + d.GetMessage()
                        );
                    }

                    if (!success)
                    {
                        Log("Compilation failed!");
                        throw new Exception("Compilation failed!");
                    }
                    else
                    {
                        if (url != null)
                        {
                            using (var f = File.OpenWrite(url))
                            {
                                var b = stream.GetBuffer();
                                f.Write(b, 0, b.Length - 1);
                                f.Flush();
                            }
                        }
                    }
                }
            }
            else
            {
                success = false;
            }
        }

        return success;
    }

    static void Log(string message)
    {
        Console.WriteLine(message);
    }


}