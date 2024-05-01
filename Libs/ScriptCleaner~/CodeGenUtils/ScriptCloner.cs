using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nomnom.CodeGenUtils {
    public static class ScriptCloner {
        // private readonly static string[] SupportedTypes = {
        //     "MonoBehaviour",
        //     "NetworkBehaviour",
        //     "ScriptableObject",
        //     "EnemyAI"
        // };

        public static string[] Clone(string assetRipperPath, string dataPath, string[] files, string[] supportedTypes, string fileTemplate, Action<string> log, string[] assemblies) {
            var usedFiles = new List<string>();
            var badFiles = new List<string>();
            foreach (var file in files) {
                try {
                    var text = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(text);
                    var root = tree.GetRoot();

                    // Create a compilation with references
                    var compilation = CSharpCompilation.Create("UnityGame")
                        .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
                    foreach (var assembly in assemblies) {
                        compilation = compilation.AddReferences(MetadataReference.CreateFromFile(assembly));
                    }
                    compilation = compilation.AddSyntaxTrees(tree);

                    // Get the semantic model
                    var semanticModel = compilation.GetSemanticModel(tree);
                    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();

                    var foundOne = false;
                    var firstClass = classes.FirstOrDefault();
                    if (firstClass == null) {
                        badFiles.Add(file);
                        continue;
                    }
                    
                    var modifiers = firstClass.Modifiers;
                    var isPublic = modifiers.Any(x => x.Kind() == SyntaxKind.PublicKeyword);
                    var isNotAbstract = !modifiers.Any(x => x.Kind() == SyntaxKind.AbstractKeyword);
                    var isNotStatic = !modifiers.Any(x => x.Kind() == SyntaxKind.StaticKeyword);

                    if (!isPublic || !isNotAbstract || !isNotStatic) {
                        badFiles.Add(file);
                        continue;
                    }

                    var namespaceDeclaration = firstClass.Parent as NamespaceDeclarationSyntax;
                    string namespaceName = null;
                    if (namespaceDeclaration != null) {
                        namespaceName = namespaceDeclaration.Name.ToString();
                        // log($"Namespace: {namespaceName} for {file}");
                        if (namespaceName != "GameNetcodeStuff" && namespaceName != "Dissonance.Integrations.Unity_NFGO") {
                            badFiles.Add(file);
                            continue;
                        }
                    }

                    foreach (var c in classes) {
                        var symbol = semanticModel.GetDeclaredSymbol(c);
                        if (symbol == null) continue;

                        if (!supportedTypes.Where(x => !x.StartsWith("I")).Any(x => InheritsFromType(symbol, x))) {
                            continue;
                        }

                        log(c.Identifier.ToString());

                        // make directory
                        var relativePath = file.Replace(Path.Combine(assetRipperPath, "Assets"), dataPath);
                        log($"Creating {relativePath} at {Path.GetDirectoryName(relativePath)}");
                        
                        var directory = Path.GetDirectoryName(relativePath);
                        if (directory == null) {
                            badFiles.Add(file);
                            continue;
                        }
                        Directory.CreateDirectory(directory);

                        // create file
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var projectFilePath = relativePath;
                        var className = fileName;
                        var fileContents = fileTemplate
                            .Replace("$CLASS_NAME$", className)
                            .Replace("$BASE_CLASS$", $"{(string.IsNullOrEmpty(namespaceName) ? "global::" : $"{namespaceName}.")}" + className);

                        File.WriteAllText(projectFilePath, fileContents);

                        usedFiles.Add(file);
                        foundOne = true;
                        break;
                    }

                    if (!foundOne) {
                        badFiles.Add(file);
                    }
                } catch (Exception e) {
                    log($"Error: {e}");
                    badFiles.Add(file);
                }
            }
            
            foreach (var badFile in badFiles) {
                if (File.Exists(badFile)) {
                    File.Delete(badFile);
                }
            }
            
            return usedFiles.ToArray();
        }

        private static bool InheritsFromType(ITypeSymbol? typeSymbol, string type) {
            if (typeSymbol == null) {
                return false;
            }

            if (typeSymbol.Name == type) {
                return true;
            }

            return InheritsFromType(typeSymbol.BaseType, type);
        }
    }
}
