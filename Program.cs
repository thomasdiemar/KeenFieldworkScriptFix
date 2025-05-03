using Microsoft.Build.Evaluation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.MSBuild;

// See https://aka.ms/new-console-template for more information
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Sandbox.ModAPI.Ingame;
using System.Security.Cryptography.Xml;
using VRageMath;

internal class Program
{
    private static void Main(string[] args)
    {
        var sourceCodeFilePath = @"C:\Users\thoma\source\repos\Program.OreScoutDrone\SpaceEngineers.TurretsTargetAcquistion\SEScriptBuilder\Merge.cs";
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

            var info = ProjectInfo.Create(
                projectId,
                version: VersionStamp.Default,
                name: "KeenFixProject",
                assemblyName: "KeenFixProject.dll",
                language: LanguageNames.CSharp,
                compilationOptions: DefaultCompilationOptions);

            var project = workspace.AddProject(info);

            var text = SourceText.From(sourceCode);

            var document = workspace.AddDocument(project.Id, "KeenFixProject.cs", text);

            var success = workspace.TryApplyChanges(document.Project.Solution);

            List< MetadataReference> references = new List<MetadataReference>();
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

            var solution = document.Project.Solution.AddMetadataReferences(projectId, references);


            CompileSolution(solution, resultCodeFilePath);
        }

        bool CompileSolution(Solution Solution, string url = null)
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

        void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}