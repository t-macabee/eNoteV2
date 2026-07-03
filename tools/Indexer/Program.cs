using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynIndexer;

record DirtyManifest(int SchemaVersion, List<string> DirtyFiles, List<string> DeletedFiles, string MarkedAt);


public class Program
{
    private const int SchemaVersion = 1;

    private static readonly string GitRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    private static readonly string CodeAuditDir = Path.Combine(GitRoot, ".codeaudit");
    private static readonly string SemanticDir = Path.Combine(CodeAuditDir, "semantic");
    private static readonly string DirtyFilePath = Path.Combine(CodeAuditDir, "dirty-files.json");

    private static readonly string SolutionPath = @"C:\Users\Tarik\Desktop\eNoteV2\eNote\eNote.sln";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task Main(string[] args)
    {
        EnsureDirectories();

        var mode = args.FirstOrDefault(a => a.StartsWith("--mode="))?.Split('=', 2)[1] ?? "help";

        try
        {
            switch (mode)
            {
                case "load":
                    var sol = await LoadSolutionAsync();
                    Console.WriteLine($"Loaded {sol.Projects.Count()} projects.");
                    break;

                case "fingerprint":
                    var fpSymbol = args.FirstOrDefault(a => a.StartsWith("--symbol="))?.Split('=', 2)[1];
                    if (string.IsNullOrEmpty(fpSymbol))
                        Console.WriteLine("Usage: --mode=fingerprint --symbol=Namespace.ClassName");
                    else
                        await ComputeFingerprintOnlyAsync(fpSymbol);
                    break;

                case "recompute-all":
                    await RecomputeAllFingerprintsAsync();
                    break;

                case "mark-dirty":
                    await MarkDirtyAsync(args);
                    Console.WriteLine("Manifest updated.");
                    break;

                case "sweep":
                    await SweepAsync();
                    break;

                case "status":
                    ShowStatus();
                    break;

                case "lint":
                    await LintModeAsync();
                    break;

                case "impact":
                    await ImpactModeAsync();
                    break;

                default:
                    Console.WriteLine("Commands:");
                    Console.WriteLine("  --mode=load");
                    Console.WriteLine("  --mode=fingerprint --symbol=X");
                    Console.WriteLine("  --mode=recompute-all");
                    Console.WriteLine("  --mode=mark-dirty --files=PATH [--deleted=PATH]");
                    Console.WriteLine("  --mode=sweep");
                    Console.WriteLine("  --mode=status");
                    Console.WriteLine("  --mode=lint");
                    Console.WriteLine("  --mode=impact");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex}");
            Environment.Exit(1);
        }
    }

    private static void EnsureDirectories()
    {
        Directory.CreateDirectory(CodeAuditDir);
        Directory.CreateDirectory(SemanticDir);
    }

    private static string SanitizeId(string symbolId) =>
        symbolId.Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace(", ", "_").Replace(",", "_");

    private static string GetRelativePath(string? fullPath)
    {
        if (fullPath == null) return "";
        var gitRoot = GitRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetRelativePath(gitRoot, fullPath).Replace('\\', '/');
    }

    private static async Task<Solution> LoadSolutionAsync()
    {
        MSBuildLocator.RegisterDefaults();
        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                Console.Error.WriteLine($"Load warning: {e.Diagnostic.Message}");
        };

        var solution = await workspace.OpenSolutionAsync(SolutionPath);

        var failures = workspace.Diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToList();

        if (failures.Any())
            throw new InvalidOperationException(
                $"Solution load failed ({failures.Count} errors):\n" +
                string.Join("\n", failures.Select(f => f.Message))
            );

        return solution;
    }

    private static async Task<INamedTypeSymbol?> FindTypeSymbolAsync(Solution solution, string name)
    {
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            var type = compilation.GetTypeByMetadataName(name);
            if (type != null) return type;
        }
        return null;
    }

    private static string ComputeFingerprint(ISymbol symbol, Compilation compilation)
    {
        var parts = new List<string>
        {
            symbol.Name,
            symbol.DeclaredAccessibility.ToString()
        };

        if (symbol is IMethodSymbol method)
        {
            parts.Add(method.ReturnType.ToDisplayString());
            parts.Add(string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString())));
        }

        if (symbol is INamedTypeSymbol namedType)
        {
            var refs = namedType.GetMembers()
                .OfType<ITypeSymbol>()
                .Select(ResolveTypeName)
                .OrderBy(x => x);
            parts.AddRange(refs);

            var publicMethods = namedType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsStatic && m.MethodKind == MethodKind.Ordinary)
                .Select(m =>
                {
                    var returnType = m.ReturnType?.ToDisplayString() ?? "void";
                    var paramTypes = string.Join(",", m.Parameters.Select(p => p.Type?.ToDisplayString() ?? "?"));
                    return $"{m.Name}({paramTypes})->{returnType}";
                })
                .OrderBy(x => x);
            parts.AddRange(publicMethods);
        }

        var canonicalized = string.Join("|", parts);
        var hash = XxHash3.Hash(Encoding.UTF8.GetBytes(canonicalized));
        return Convert.ToHexString(hash);
    }

    private static string ResolveTypeName(ITypeSymbol type)
    {
        if (type is IErrorTypeSymbol error)
            return $"UNRESOLVED:{error.Name}";

        return type.ToDisplayString();
    }

    private static async Task ComputeFingerprintOnlyAsync(string symbolName)
    {
        var solution = await LoadSolutionAsync();
        var symbol = await FindTypeSymbolAsync(solution, symbolName);
        if (symbol == null)
        {
            Console.WriteLine($"Symbol not found: {symbolName}");
            return;
        }

        var compilation = solution.Projects
            .First(p => p.Documents.Any(d => symbol.Locations.Any(l => l.SourceTree?.FilePath == d.FilePath)))
            .GetCompilationAsync().Result!;

        var fp = ComputeFingerprint(symbol, compilation);
        Console.WriteLine($"Fingerprint: {fp}");
    }

    private static async Task RecomputeAllFingerprintsAsync()
    {
        var semanticFiles = Directory.GetFiles(SemanticDir, "*.semantic.json");
        if (semanticFiles.Length == 0)
        {
            Console.WriteLine("No semantic files found.");
            return;
        }

        Console.WriteLine($"Recomputing fingerprints for {semanticFiles.Length} curated symbol(s)...");

        var curatedSymbols = new Dictionary<string, string>();
        foreach (var sf in semanticFiles)
        {
            var text = await File.ReadAllTextAsync(sf);
            var node = JsonNode.Parse(text)?.AsObject();
            var symbolId = node?["symbolId"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(symbolId))
                curatedSymbols[symbolId] = sf;
        }

        Console.WriteLine("  Loading solution...");
        var solution = await LoadSolutionAsync();

        var updated = 0;
        var matched = new HashSet<string>();

        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null) continue;

            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync();
                if (syntaxTree == null) continue;

                var root = await syntaxTree.GetRootAsync();
                var model = compilation.GetSemanticModel(syntaxTree);

                foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol namedSymbol) continue;

                    var symbolId = namedSymbol.ToDisplayString();
                    if (!curatedSymbols.TryGetValue(symbolId, out var sfPath)) continue;
                    if (!matched.Add(symbolId)) continue;

                    var newFingerprint = ComputeFingerprint(namedSymbol, compilation);

                    var fileText = await File.ReadAllTextAsync(sfPath);
                    var node = JsonNode.Parse(fileText)?.AsObject();
                    if (node == null) continue;

                    var oldFingerprint = node["fingerprint"]?.GetValue<string>();
                    if (newFingerprint == oldFingerprint)
                    {
                        Console.WriteLine($"  [SAME]    {symbolId}");
                        continue;
                    }

                    node["fingerprint"] = newFingerprint;
                    var json = node.ToJsonString(JsonOptions);
                    var tmp = sfPath + ".tmp";
                    await File.WriteAllTextAsync(tmp, json);
                    File.Move(tmp, sfPath, overwrite: true);

                    Console.WriteLine($"  [UPDATED] {symbolId}");
                    updated++;
                }
            }
        }

        var unmatched = curatedSymbols.Keys.Except(matched).ToList();
        if (unmatched.Count > 0)
        {
            Console.WriteLine("  [WARN] Could not locate in compilation (manual update needed):");
            foreach (var u in unmatched)
                Console.WriteLine($"    {u}");
        }

        Console.WriteLine($"Recompute complete. Updated: {updated}/{semanticFiles.Length}.");
    }

    private static async Task MarkDirtyAsync(string[] args)
    {
        var filesPath = args.FirstOrDefault(a => a.StartsWith("--files="))?.Split('=', 2)[1];
        var deletedPath = args.FirstOrDefault(a => a.StartsWith("--deleted="))?.Split('=', 2)[1];

        var dirtyFiles = filesPath != null && File.Exists(filesPath)
            ? (await File.ReadAllLinesAsync(filesPath))
                .Select(l => l.Trim().Replace('\\', '/'))
                .Where(l => l.Length > 0)
                .ToList()
            : new List<string>();

        var deletedFiles = deletedPath != null && File.Exists(deletedPath)
            ? (await File.ReadAllLinesAsync(deletedPath))
                .Select(l => l.Trim().Replace('\\', '/'))
                .Where(l => l.Length > 0)
                .ToList()
            : new List<string>();

        var existing = new DirtyManifest(SchemaVersion, [], [], "");

        if (File.Exists(DirtyFilePath))
        {
            var text = await File.ReadAllTextAsync(DirtyFilePath);
            existing = JsonSerializer.Deserialize<DirtyManifest>(text, JsonOptions) ?? existing;
        }

        var merged = new DirtyManifest(
            SchemaVersion: SchemaVersion,
            DirtyFiles: (existing.DirtyFiles ?? []).Union(dirtyFiles).Distinct().ToList(),
            DeletedFiles: (existing.DeletedFiles ?? []).Union(deletedFiles).Distinct().ToList(),
            MarkedAt: DateTime.UtcNow.ToString("O")
        );

        var json = JsonSerializer.Serialize(merged, JsonOptions);
        var tmp = DirtyFilePath + ".tmp";

        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, DirtyFilePath, overwrite: true);
    }

    private static async Task SweepAsync()
    {
        if (!File.Exists(DirtyFilePath))
        {
            Console.WriteLine("No dirty-files.json found — nothing to sweep.");
            return;
        }

        var manifestText = await File.ReadAllTextAsync(DirtyFilePath);
        var manifest = JsonSerializer.Deserialize<DirtyManifest>(manifestText, JsonOptions);

        if (manifest == null || ((manifest.DirtyFiles == null || manifest.DirtyFiles.Count == 0) && (manifest.DeletedFiles == null || manifest.DeletedFiles.Count == 0)))
        {
            Console.WriteLine("Manifest is empty — nothing to sweep.");
            return;
        }

        Console.WriteLine($"Sweeping {manifest.DirtyFiles?.Count ?? 0} dirty, {manifest.DeletedFiles?.Count ?? 0} deleted...");


        var sourceFileToSymbolIds = new Dictionary<string, List<string>>();
        var curatedFingerprints = new Dictionary<string, string>();
        var dependentSymbolIds = new Dictionary<string, List<string>>();
        
        var semanticFiles = Directory.GetFiles(SemanticDir, "*.semantic.json");
        foreach (var sf in semanticFiles)
        {
            var text = await File.ReadAllTextAsync(sf);
            var node = JsonNode.Parse(text)?.AsObject();
            if (node == null) continue;

            var symbolId = node["symbolId"]?.GetValue<string>();
            var fingerprint = node["fingerprint"]?.GetValue<string>();
            var sourceFile = node["facts"]?["sourceFile"]?.GetValue<string>();

            if (symbolId == null || fingerprint == null || sourceFile == null) continue;

            if (!sourceFileToSymbolIds.ContainsKey(sourceFile))
                sourceFileToSymbolIds[sourceFile] = new List<string>();
            sourceFileToSymbolIds[sourceFile].Add(symbolId);
            curatedFingerprints[symbolId] = fingerprint;

            var collaborators = node["interpretation"]?["collaborators"]?.AsArray();
            if (collaborators != null)
            {
                foreach (var c in collaborators)
                {
                    var depSymbol = c?["symbol"]?.GetValue<string>();
                    if (depSymbol == null) continue;
                    if (!dependentSymbolIds.ContainsKey(depSymbol))
                        dependentSymbolIds[depSymbol] = new List<string>();
                    if (!dependentSymbolIds[depSymbol].Contains(symbolId))
                        dependentSymbolIds[depSymbol].Add(symbolId);
                }
            }
        }

        var curatedSymbolIds = new HashSet<string>(curatedFingerprints.Keys);
        Console.WriteLine($"  Curated entries: {curatedSymbolIds.Count} symbols across {semanticFiles.Length} semantic files.");

        var processed = new List<string>();
        foreach (var deletedFile in manifest.DeletedFiles ?? [])
        {
            if (sourceFileToSymbolIds.TryGetValue(deletedFile, out var symbolIds))
            {
                foreach (var sid in symbolIds)
                {
                    await FlagSemanticStaleAsync(sid, "source_deleted");
                    await FlagDependentsStaleAsync(sid, dependentSymbolIds);
                }
            }
            processed.Add(deletedFile);
        }

        var relevantDirtyFiles = (manifest.DirtyFiles ?? [])
            .Where(f => sourceFileToSymbolIds.ContainsKey(f))
            .ToList();

        Console.WriteLine($"  Relevant dirty files (matching curated symbols): {relevantDirtyFiles.Count}");

        if (relevantDirtyFiles.Count > 0)
        {
            Console.WriteLine("  Loading solution...");
            var solution = await LoadSolutionAsync();

            foreach (var dirtyFile in relevantDirtyFiles)
            {
                var documents = solution.Projects
                    .SelectMany(p => p.Documents)
                    .Where(d => GetRelativePath(d.FilePath ?? "") == dirtyFile)
                    .ToList();

                foreach (var doc in documents)
                {
                    var compilation = await doc.Project.GetCompilationAsync();
                    if (compilation == null) continue;

                    var syntaxTree = await doc.GetSyntaxTreeAsync();
                    if (syntaxTree == null) continue;

                    var root = await syntaxTree.GetRootAsync();
                    var model = compilation.GetSemanticModel(syntaxTree);

                    var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();
                    foreach (var typeDecl in typeDeclarations)
                    {
                        var declaredSymbol = model.GetDeclaredSymbol(typeDecl);
                        if (declaredSymbol is not INamedTypeSymbol namedSymbol) continue;

                        var symbolId = namedSymbol.ToDisplayString();
                        if (!curatedSymbolIds.Contains(symbolId)) continue;

                        var currentFingerprint = ComputeFingerprint(namedSymbol, compilation);
                        if (curatedFingerprints.TryGetValue(symbolId, out var storedFingerprint)
                            && currentFingerprint != storedFingerprint)
                        {
                            Console.WriteLine($"  Fingerprint changed for: {symbolId}");
                            await FlagSemanticStaleAsync(symbolId, "fingerprint_changed");
                            await FlagDependentsStaleAsync(symbolId, dependentSymbolIds);
                        }
                    }
                }

                processed.Add(dirtyFile);
            }
        }

        foreach (var dirtyFile in (manifest.DirtyFiles ?? []).Except(processed))
        {
            processed.Add(dirtyFile);
        }
        
        var remaining = new DirtyManifest(
            SchemaVersion: SchemaVersion,
            DirtyFiles: (manifest.DirtyFiles ?? []).Except(processed).ToList(),
            DeletedFiles: (manifest.DeletedFiles ?? []).Except(processed).ToList(),
            MarkedAt: DateTime.UtcNow.ToString("O")
        );

        var json = JsonSerializer.Serialize(remaining, JsonOptions);
        var tmp = DirtyFilePath + ".tmp";

        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, DirtyFilePath, overwrite: true);

        Console.WriteLine("Sweep complete.");
    }
    
    private static async Task FlagSemanticStaleAsync(string symbolId, string reason)
    {
        var semanticPath = Path.Combine(SemanticDir, $"{SanitizeId(symbolId)}.semantic.json");
        if (!File.Exists(semanticPath))
            return;

        var text = await File.ReadAllTextAsync(semanticPath);
        var node = JsonNode.Parse(text)?.AsObject();
        if (node == null)
            return;

        if (node["status"]?.GetValue<string>() == "stale")
            return;

        node["status"] = "stale";
        node["staleReason"] = reason;

        var json = node.ToJsonString(JsonOptions);
        var tmp = semanticPath + ".tmp";

        await File.WriteAllTextAsync(tmp, json);
        File.Move(tmp, semanticPath, overwrite: true);

        Console.WriteLine($"  Semantic entry flagged stale ({reason}): {symbolId}");
    }

    private static async Task FlagDependentsStaleAsync(string symbolId, Dictionary<string, List<string>> dependentSymbolIds)
    {
        if (!dependentSymbolIds.TryGetValue(symbolId, out var dependents)) return;
        foreach (var dep in dependents)
            await FlagSemanticStaleAsync(dep, $"dependency_stale:{symbolId}");
    }

    private static async Task LintModeAsync()
    {
        var semanticFiles = Directory.GetFiles(SemanticDir, "*.semantic.json");
        var foundIssues = false;
        var pascalRegex = new System.Text.RegularExpressions.Regex(@"^[A-Z][a-z]+(?:[A-Z][a-z]+)*$");

        foreach (var sf in semanticFiles)
        {
            string text;
            try { text = await File.ReadAllTextAsync(sf); }
            catch (Exception ex) { Console.Error.WriteLine($"ERROR reading {sf}: {ex.Message}"); continue; }

            JsonNode? root;
            try { root = JsonNode.Parse(text); }
            catch (Exception ex) { Console.Error.WriteLine($"ERROR parsing {sf}: {ex.Message}"); continue; }

            var collaborators = root?["interpretation"]?["collaborators"]?.AsArray();
            if (collaborators == null) continue;

            for (int i = 0; i < collaborators.Count; i++)
            {
                var rel = collaborators[i]?["relationship"]?.GetValue<string>();
                if (rel != null && !pascalRegex.IsMatch(rel))
                {
                    Console.WriteLine($"[WARN] {Path.GetFileName(sf)}: collaborators[{i}].relationship = \"{rel}\" — expected PascalCase");
                    foundIssues = true;
                }
            }
        }

        if (!foundIssues)
            Console.WriteLine("Lint OK — all relationship values use PascalCase.");

        // Exit with 0 even when violations found
    }

    private static async Task ImpactModeAsync()
    {
        if (!File.Exists(DirtyFilePath))
        {
            Console.WriteLine("[]");
            return;
        }

        var manifestText = await File.ReadAllTextAsync(DirtyFilePath);
        var manifest = JsonSerializer.Deserialize<DirtyManifest>(manifestText, JsonOptions);

        if (manifest == null || (manifest.DirtyFiles == null || manifest.DirtyFiles.Count == 0))
        {
            Console.WriteLine("[]");
            return;
        }

        var dirtyFiles = new HashSet<string>(manifest.DirtyFiles);
        var semanticFiles = Directory.GetFiles(SemanticDir, "*.semantic.json");
        var results = new List<object>();

        foreach (var sf in semanticFiles)
        {
            string text;
            try { text = await File.ReadAllTextAsync(sf); }
            catch { continue; }

            JsonNode? node;
            try { node = JsonNode.Parse(text); }
            catch { continue; }

            var symbolId = node?["symbolId"]?.GetValue<string>();
            var sourceFile = node?["facts"]?["sourceFile"]?.GetValue<string>();
            if (symbolId == null || sourceFile == null) continue;

            // Direct match: the dirty file IS this symbol's source file
            if (dirtyFiles.Contains(sourceFile))
            {
                results.Add(new { semanticFile = $"{SanitizeId(symbolId)}.semantic.json", reason = "direct", via = symbolId });
                continue;
            }

            // Dependency match: the dirty file's symbol appears as a collaborator
            var collaborators = node?["interpretation"]?["collaborators"]?.AsArray();
            if (collaborators == null) continue;

            foreach (var c in collaborators)
            {
                var depSymbol = c?["symbol"]?.GetValue<string>();
                if (depSymbol == null) continue;
                // Check if depSymbol is a curated symbol whose sourceFile is dirty
                // We use the fact that depSymbol might map to a semantic file with a known sourceFile
                var depSanitized = SanitizeId(depSymbol);
                var depPath = Path.Combine(SemanticDir, $"{depSanitized}.semantic.json");
                if (File.Exists(depPath))
                {
                    try
                    {
                        var depText = await File.ReadAllTextAsync(depPath);
                        var depNode = JsonNode.Parse(depText);
                        var depSource = depNode?["facts"]?["sourceFile"]?.GetValue<string>();
                        if (depSource != null && dirtyFiles.Contains(depSource))
                        {
                            results.Add(new { semanticFile = $"{SanitizeId(symbolId)}.semantic.json", reason = "dependency", via = depSymbol });
                            break;
                        }
                    }
                    catch { continue; }
                }
            }
        }

        var output = JsonSerializer.Serialize(results, JsonOptions);
        Console.WriteLine(output);
    }

    private static void ShowStatus()
    {
        Console.WriteLine("RoslynIndexer is ready.");
        Console.WriteLine($"  Git root: {GitRoot}");
        Console.WriteLine($"  CodeAudit dir: {CodeAuditDir}");

        if (File.Exists(DirtyFilePath))
            Console.WriteLine($"  Dirty manifest: exists ({new FileInfo(DirtyFilePath).Length} bytes)");
        else
            Console.WriteLine("  Dirty manifest: (none)");

        if (Directory.Exists(SemanticDir))
        {
            var semanticFiles = Directory.GetFiles(SemanticDir, "*.semantic.json");
            Console.WriteLine($"  Curated semantic entries: {semanticFiles.Length}");
        }
    }
}
