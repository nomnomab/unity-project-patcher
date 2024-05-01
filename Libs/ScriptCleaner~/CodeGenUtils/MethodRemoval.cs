using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nomnom.CodeGenUtils {
    public static class MethodRemoval {
        public static void Scrub(string[] files, Func<NodeInfo, bool> canRemoveFunction, Action<string> log) {
            foreach (var file in files) {
                if (!File.Exists(file)) continue;
                
                var text = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(text);
                var root = tree.GetRoot();
                var nodesToRemove = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Select(x => (x, new NodeInfo(file, x)))
                    .Where(x => canRemoveFunction(x.Item2))
                    .Select(x => x.x);
                
                var newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                var rewriter = new RemoveCtorMethodCalls();
                newRoot = rewriter.Visit(newRoot);
                
                var newCode = newRoot.ToFullString();
                File.WriteAllText(file, newCode);
            }
        }

        public readonly struct NodeInfo {
            public readonly string file;
            public readonly string type;
            public readonly string indentifier;
            public readonly string text;
            
            public NodeInfo(string file, MethodDeclarationSyntax node) {
                this.file = file;
                type = node.GetType().Name;
                indentifier = node.Identifier.Text;
                text = node.ToFullString();
            }
        }
    }
}