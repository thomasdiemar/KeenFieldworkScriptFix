using Microsoft.Build.Evaluation;
using Microsoft.Build.Graph;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;

// See https://aka.ms/new-console-template for more information
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.EntityComponents.Blocks;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Security.Cryptography.Xml;
using System.Xml.Linq;
using VRageMath;
using VRageRender.Voxels;

internal class Program
{
    //private static void Main(string[] args)
    //{
    //    Log("Start");
    //    Test2();
    //}

    private static async Task Main(string[] args)
    {
        Log("Start");
       //await Test1();
       await Test3();
    }

    static async Task Test1()
    {
        //var solution = CreateTempSolutionFromScript(@"C:\Users\thoma\source\repos\Program.OreScoutDrone\SpaceEngineers.TurretsTargetAcquistion\SEScriptBuilder\Merge.cs");
        var solution = CreateTempSolutionFromScript(@"C:\Users\thoma\source\repos\Program.OreScoutDrone\Program.SimpleScripts\KeenFixTest.cs");

        //await GetInterfaces(solution);

        var dataList = new List<Data>();
        await GetInterfacesAndAbstractClasses(solution, dataList);

        Print(dataList);

        await GetDerives(dataList);
        //var wrappers = await GenerateWrappers(dataList);

        //Print(wrappers);

        //await ReplaceWithWrappers(wrappers);
    } 

    static async Task Test3()
    {
        var solution = await LoadSolution(@"C:\Users\thoma\source\repos\Program.OreScoutDrone\Program.OreScoutDrone.sln");
        //var solution = CreateTempSolutionFromScript(@"C:\Users\thoma\source\repos\Program.OreScoutDrone\Program.SimpleScripts\KeenFixTest.cs");
  

        var dataList = new List<Data>();
        await GetInterfacesAndAbstractClasses(solution, dataList);

        Print(dataList);
    
       // var wrappers = await GenerateWrappers(dataList);

        //Print(wrappers);
    }

    //static async Task test(Solution solution, string module = null)
    //{
    //    ProjectDependencyGraph projectGraph = solution.GetProjectDependencyGraph();
    //    foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
    //    {
    //        var project = solution.GetProject(projectId);
    //        if (project != null)
    //        {
    //            var moduledll = module ?? project.AssemblyName;
    //            moduledll += ".dll";
    //            Console.WriteLine(moduledll);

    //            Compilation projectCompilation = project.GetCompilationAsync().Result;

    //            var collect = CollectFromInterfaces(projectCompilation, moduledll);
    //            GenerateWrappers(collect);
    //        }
    //    }

    //}

    
    static void Print(string documentName, List<MethodDeclarationSyntax> MethodDeclarationSyntax)
    {
        foreach (var method in MethodDeclarationSyntax)
        {
            //method.Parent
            Console.Write(documentName + " ");
            Console.Write((method.Parent as TypeDeclarationSyntax).Identifier.ToString());
            Console.Write(" " + method.Identifier.ToString());
            Console.WriteLine(" " + method.ReturnType.ToString());
        }
    }

    static void Print(List<Data> dataList)
    {
        foreach (var data in dataList)
        {
            if(data.Document.Name.ToString().Contains("Compress") || data.Document.Name.ToString().Contains("Minify")) continue;
            Print(data.Document.Name, data.MethodDeclarationSyntax);
        }
    }

    static void Print(string documentName, Dictionary<string, WrapperData> wrappers)
    {
        foreach (var wrapper in wrappers)
        {
            Console.WriteLine(wrapper.Key);
            //Console.WriteLine(" - "+wrapper.Value.TypeDeclarationSyntax.Identifier.ToFullString());
            Print(documentName, wrapper.Value.MethodDeclarationSyntax);
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
        return project.Solution;
    }

    static Solution CreateTempSolutionFromScript(string sourceCodeFilePath)
    {
        //var sourceCodeFilePath = @"C:\Users\thoma\source\repos\Program.OreScoutDrone\SpaceEngineers.TurretsTargetAcquistion\SEScriptBuilder\Merge.cs";
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
            var modulename = "KeenFixProject";
            var module = modulename + ".dll";

            var info = ProjectInfo.Create(
                projectId,
                version: VersionStamp.Default,
                name: modulename,
                assemblyName: modulename,
                language: LanguageNames.CSharp,
                compilationOptions: DefaultCompilationOptions);

            var project = workspace.AddProject(info);

            var text = SourceText.From(sourceCode);

            var document = workspace.AddDocument(project.Id, modulename+".cs", text);


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

    class Data
    {
        public Microsoft.CodeAnalysis.Document Document;
        public List<MethodDeclarationSyntax> MethodDeclarationSyntax;
        internal List<MethodDeclarationSyntax> OverrideMethodDeclarationSyntax;
    }

    class WrapperData : Data
    {
        public TypeDeclarationSyntax TypeDeclarationSyntax;
    }

    static async Task GetInterfacesAndAbstractClasses(Solution solution, List<Data> dataList)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;
                var semanticModel = await document.GetSemanticModelAsync();
                if (semanticModel == null) ;

                var data = new Data { Document = document, MethodDeclarationSyntax = new List<MethodDeclarationSyntax>(), OverrideMethodDeclarationSyntax = new List<MethodDeclarationSyntax>() };

                GetInterfaces(syntaxRoot, semanticModel, data);
                GetAbstractClasses(syntaxRoot, semanticModel, data);
                GetOverrideClasses(syntaxRoot, semanticModel, data);

                dataList.Add(data);
            }
        }

    }

    static async Task GetDerives(List<Data> dataList)
    {
        foreach (var data in dataList)
        {
            var model = await data.Document.GetSemanticModelAsync();
            if (model == null) continue;

            foreach (var parent in data.MethodDeclarationSyntax)
            {
                var targetMethodSymbol = model.GetDeclaredSymbol(parent);
                if (targetMethodSymbol == null) continue;

                var declaringType = targetMethodSymbol.ContainingType;

                foreach (var data2 in dataList)
                {
                    var model2 = await data2.Document.GetSemanticModelAsync();
                    if (model2 == null) continue;

                    foreach (var potentialoverride in data2.OverrideMethodDeclarationSyntax)
                    {
                        var classSymbol = model2.GetDeclaredSymbol(potentialoverride);
                        if (classSymbol == null) continue;

                        var classType = classSymbol.ContainingType;

                        if (DerivesFromOrImplements(classType, declaringType))
                        {
                            if (classType.ToString().Contains("Compress") || classType.ToString().Contains("Minify")) continue;
                            Console.WriteLine("DerivesFromOrImplements " + classType.ToString() + " " + declaringType.ToString() + "@" + potentialoverride.Identifier.ToString());
                        }
                    }
                }
            }
        }
    }

    static bool DerivesFromOrImplements(INamedTypeSymbol classSymbol, INamedTypeSymbol baseType)
    {
        // Base class check
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }

        // Interface check
        return classSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, baseType));
    }


    static void GetInterfaces(SyntaxNode syntaxRoot, SemanticModel semanticModel, Data data)
    {
        var interfaceDeclarations = syntaxRoot.DescendantNodes()
            .OfType<InterfaceDeclarationSyntax>();

        foreach (var iface in interfaceDeclarations)
        {
            GetGenericReturnMethods(iface, semanticModel, data.MethodDeclarationSyntax.Add);
        }
    }

    static void GetAbstractClasses(SyntaxNode syntaxRoot, SemanticModel semanticModel, Data data)
    {
        var abstractClasses = syntaxRoot
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(cls => cls.Modifiers.Any(SyntaxKind.AbstractKeyword));

        foreach (var absClass in abstractClasses)
        {
            GetGenericReturnMethods(absClass, semanticModel, data.MethodDeclarationSyntax.Add);
        }
    }

    static void GetOverrideClasses(SyntaxNode syntaxRoot, SemanticModel semanticModel, Data data)
    {
        var classes = syntaxRoot
         .DescendantNodes()
         .OfType<ClassDeclarationSyntax>()
         .Where(cls => !cls.Modifiers.Any(SyntaxKind.AbstractKeyword));

        foreach (var classDecl in classes)
        {
            GetGenericReturnMethods(classDecl, semanticModel, data.OverrideMethodDeclarationSyntax.Add);
        }
    }

    static void GetGenericReturnMethods(TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel semanticModel, Action<MethodDeclarationSyntax> add)
    {
        //var namespaceName = typeDeclarationSyntax.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
        //var fullName = !string.IsNullOrEmpty(namespaceName) ? $"{namespaceName}.{typeDeclarationSyntax.Identifier}" : typeDeclarationSyntax.Identifier.ToString();
 
        var hasGenericReturnMethod = typeDeclarationSyntax.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
            {
                var returnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
                return returnType is INamedTypeSymbol namedType && namedType.IsGenericType;
            });

        foreach (var method in hasGenericReturnMethod)
        {
            add(method);
        }
    }

    

    static async Task<Dictionary<string, WrapperData>> GenerateWrappers(List<Data> dataList)
    {
        var wrappers = new Dictionary<string, WrapperData>();
        
        foreach (var data in dataList)
        {
            var semanticModel = await data.Document.GetSemanticModelAsync();

            foreach (var method in data.MethodDeclarationSyntax)
            {
                var type = semanticModel.GetTypeInfo(method.ReturnType).Type;

                var typeName = type
                    .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", string.Empty);

                GenericNameSyntax returntype;
                if (method.ReturnType is QualifiedNameSyntax qualifiedNameSyntax)
                {
                    returntype = qualifiedNameSyntax.Right as GenericNameSyntax;
                }
                else
                {
                    returntype = method.ReturnType as GenericNameSyntax;
                }

                var typeArgsName = returntype.TypeArgumentList.Arguments
                    .Select(x => x.ToString())
                    .Aggregate((a, b) => a + b);

                var simpleName = type.Name + typeArgsName + "Wrapper";
                
                // class SimpleNameWrapper : OriginalReturnType { }
                var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(typeName));
                var classDecl = SyntaxFactory.ClassDeclaration(simpleName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)))
                    .NormalizeWhitespace();

                if(!wrappers.ContainsKey(simpleName))
                {
                    wrappers.Add(simpleName, new WrapperData { MethodDeclarationSyntax = new List<MethodDeclarationSyntax> { method }, TypeDeclarationSyntax = classDecl, Document = data.Document });
                }
                else
                {
                    wrappers[simpleName].MethodDeclarationSyntax.Add(method);
                }
            }
        }

        return wrappers;
    }


    static async Task ReplaceWithWrappers(SemanticModel semanticModel, Dictionary<string, WrapperData> wrappers)
    {
        foreach (var wrapper in wrappers)
        { 
            var editor = await DocumentEditor.CreateAsync(wrapper.Value.Document);

            foreach (var method in wrapper.Value.MethodDeclarationSyntax)
            {
                //var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;
                //if (methodSymbol == null) continue;

                var newReturnType = SyntaxFactory
                    .IdentifierName(wrapper.Value.TypeDeclarationSyntax.Identifier)
                    .WithTriviaFrom(method.ReturnType);

                editor.ReplaceNode(method.ReturnType, newReturnType);
            }

            var newDoc = editor.GetChangedDocument();
            var newRoot = await newDoc.GetSyntaxRootAsync();

            // Optionally format
            //newRoot = Formatter.Format(newRoot, workspace);

            //Console.WriteLine(newRoot.ToFullString());
            File.WriteAllText( @"C:\Users\thoma\MergeFix.cs", newRoot.ToFullString());


        }

       
    }
        //static async Task GetInterfaces(Solution solution)
        //{

        //    foreach (var project in solution.Projects)
        //    {
        //        Console.WriteLine($"Project: {project.Name}");

        //        foreach (var document in project.Documents)
        //        {
        //            var syntaxRoot = await document.GetSyntaxRootAsync();
        //            if (syntaxRoot == null) continue;
        //            var semanticModel = await document.GetSemanticModelAsync();
        //            if (syntaxRoot == null || semanticModel == null) continue;

        //            var interfaceDeclarations = syntaxRoot.DescendantNodes()
        //                .OfType<InterfaceDeclarationSyntax>();

        //            foreach (var iface in interfaceDeclarations)
        //            {
        //                var namespaceName = iface.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
        //                var fullName = !string.IsNullOrEmpty(namespaceName) ? $"{namespaceName}.{iface.Identifier}" : iface.Identifier.ToString();
        //                //Console.WriteLine($"  Interface: {fullName}");

        //                var hasGenericReturnMethod = iface.Members
        //                    .OfType<MethodDeclarationSyntax>()
        //                    .Where(method =>
        //                    {
        //                        var returnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
        //                        return returnType is INamedTypeSymbol namedType && namedType.IsGenericType;
        //                    });

        //                foreach (var method in hasGenericReturnMethod)
        //                {
        //                    Console.WriteLine($"Interface with generic-return method: {fullName}");
        //                    Console.WriteLine(method.ReturnType.ToFullString());
        //                }

        //            }

        //            var abstractClasses = syntaxRoot
        //              .DescendantNodes()
        //              .OfType<ClassDeclarationSyntax>()
        //              .Where(cls => cls.Modifiers.Any(SyntaxKind.AbstractKeyword));

        //            foreach (var absClass in abstractClasses)
        //            {
        //                var namespaceName = absClass.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
        //                var fullName = !string.IsNullOrEmpty(namespaceName) ? $"{namespaceName}.{absClass.Identifier}" : absClass.Identifier.ToString();
        //                //Console.WriteLine($"  Interface: {fullName}");

        //                var hasGenericReturnMethod = absClass.Members
        //                    .OfType<MethodDeclarationSyntax>()
        //                    .Where(method =>
        //                    {
        //                        var returnType = semanticModel.GetTypeInfo(method.ReturnType).Type;
        //                        return returnType is INamedTypeSymbol namedType && namedType.IsGenericType;
        //                    });

        //                foreach (var method in hasGenericReturnMethod)
        //                {
        //                    Console.WriteLine($"Abstract Class with generic-return method: {fullName}");
        //                    Console.WriteLine(method.ReturnType.ToFullString());
        //                }
        //            }
        //        }
        //    }
        //}

        //static IEnumerable<INamedTypeSymbol> GetAllInterfaces(INamespaceOrTypeSymbol ns, string module)
        //{
        //    foreach (var member in ns.GetMembers())
        //    {
        //        if (module.Equals("SpaceEngineers.Framework.DependencyInjection.dll") && member.ContainingNamespace.ToString().Equals("SpaceEngineers.Framework.DependencyInjection"))
        //        {

        //        }

        //        if (member.Name.EndsWith("IDependent"))
        //        {

        //        }
        //        if (member.ContainingModule != null && member.ContainingModule.ToString().Equals(module))
        //        {
        //            if (member is INamespaceOrTypeSymbol nested)
        //            {
        //                if (member is INamedTypeSymbol type && (type.TypeKind == TypeKind.Interface || type.TypeKind == TypeKind.Class && type.IsAbstract))
        //                {
        //                    Console.WriteLine(member.Name + " : " + member.ContainingModule);
        //                    yield return type;
        //                }
        //                else
        //                {
        //                    foreach (var sub in GetAllInterfaces(nested, module))
        //                        yield return sub;
        //                }
        //            }

        //        }
        //    }
        //}

        //static Dictionary<IMethodSymbol, ITypeSymbol> CollectFromInterfaces(Compilation compilation, string module)
        //{
        //    // Collect unique non-void return types from interfaces
        //    var returnTypes = new Dictionary<IMethodSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        //    //foreach (var syntaxtree in compilation.SyntaxTrees)
        //    //{

        //        foreach (var iface in GetAllInterfaces(compilation.GlobalNamespace, module))
        //        {
        //            foreach (var method in iface.GetMembers().OfType<IMethodSymbol>())
        //            {
        //                if (method.MethodKind == MethodKind.Ordinary && !method.ReturnsVoid)
        //                {
        //                    var type = method.ReturnType as INamedTypeSymbol;
        //                    if (type != null && type.TypeArguments.Length > 0)
        //                        returnTypes.Add(method, method.ReturnType);
        //                }
        //            }
        //        }
        //    //}
        //    return returnTypes;
        //}

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

    static async Task<bool> CompileSolution(Solution Solution, string url = null)
    {
        bool success = true;

        ProjectDependencyGraph projectGraph = Solution.GetProjectDependencyGraph();
        Dictionary<string, Stream> assemblies = new Dictionary<string, Stream>();

        foreach (ProjectId projectId in projectGraph.GetTopologicallySortedProjects())
        {
            var project = Solution.GetProject(projectId);

            Compilation projectCompilation = await project.GetCompilationAsync();
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