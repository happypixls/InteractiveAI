using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using InteractiveAI.BehaviourScripts;
using Microsoft.CodeAnalysis.Emit;
using static InteractiveAI.Utilities.ConsoleUtils;

namespace InteractiveAI.DynamicScriptLoader
{
    public class ScriptsLoader
    {
        private readonly FileSystemWatcher watcher;
        private readonly string scriptsDirectory;
        private readonly SemaphoreSlim compileLock = new SemaphoreSlim(1,1);
        private CancellationTokenSource debounceCts;
        private Assembly behaviorAssembly;
        public bool DisableWarnings { get; set; } = true;
        public List<IBehaviour> Behaviours { get; private set; }

        public ScriptsLoader(string scriptsDirectory)
        {
            Behaviours = new List<IBehaviour>();
            this.scriptsDirectory = scriptsDirectory;
            watcher = new FileSystemWatcher(scriptsDirectory, "*.cs")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            watcher.Changed += OnScriptsChangedAsync;
            watcher.Created += OnScriptsChangedAsync;
            watcher.Renamed += OnScriptsChangedAsync;
            watcher.EnableRaisingEvents = true;

            // fire-and-forget initial compile
            _ = CompileAllScriptsAsync();
        }

        private async void OnScriptsChangedAsync(object sender, FileSystemEventArgs e)
        {
            try
            {
                debounceCts?.CancelAsync();
                debounceCts = new CancellationTokenSource();
                try
                {
                    await Task.Delay(200, debounceCts.Token);
                    await CompileAllScriptsAsync();
                }
                catch (OperationCanceledException) { /* another event fired */ }
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message, ConsoleColor.Magenta);
            }
        }

        /// <summary>
        /// Valuable reference regarding resolving dependencies of parent project
        /// https://github.com/dotnet/core/issues/2082#issuecomment-442713181
        /// </summary>
        private async Task CompileAllScriptsAsync()
        {
            await compileLock.WaitAsync();
            try
            {
                Behaviours.Clear();
                PrintMessage("Starting compilation...", ConsoleColor.Blue);
                await Task.Run(() =>
                {
                    var trees = Directory
                        .GetFiles(scriptsDirectory, "*.cs")
                        .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)))
                        .ToList();

                    var references = DependencyContext
                        .Default?.CompileLibraries
                        .SelectMany(lib => lib.ResolveReferencePaths())
                        .Distinct()
                        .Select(path => MetadataReference.CreateFromFile(path))
                        .ToArray();

                    var compilation = CSharpCompilation.Create(
                        "InteractiveAI.BehaviourScripts",
                        trees,
                        references,
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                    using var ms = new MemoryStream();
                    var result = compilation.Emit(ms);

                    DisplayDiagnosticMessages(result);
                    
                    if (!result.Success) return;

                    ms.Seek(0, SeekOrigin.Begin);
                    behaviorAssembly = Assembly.Load(ms.ToArray());
                });
                
                PrintMessage("Compilation succeeded.", ConsoleColor.Green);
                
                LoadBehaviours();
            }
            finally
            {
                compileLock.Release();
            }
        }

        private void DisplayDiagnosticMessages(EmitResult result)
        {
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            PrintMessage($"<-------- Compilation result ({timestamp}) -------->", ConsoleColor.Cyan);
            
            foreach (var diag in result.Diagnostics)
            {
                var finalConsoleColor = diag.WarningLevel == 0 ? 
                    ConsoleColor.Red : 
                    ConsoleColor.Yellow;
                
                if(DisableWarnings && diag.WarningLevel != 0)
                    continue;
                
                PrintMessage(diag.ToString(), finalConsoleColor);
            }
            Console.ResetColor();
        }

        private void LoadBehaviours()
        {
            // instantiate all IBehaviour implementations
            var types = behaviorAssembly
                .GetTypes()
                .Where(t => typeof(IBehaviour).IsAssignableFrom(t)
                            && !t.IsInterface && !t.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IBehaviour)Activator.CreateInstance(type);
                instance?.Start();
                Behaviours.Add(instance);
                PrintMessage($"Loaded behaviour {type.FullName}");
            }
        }
    }
}