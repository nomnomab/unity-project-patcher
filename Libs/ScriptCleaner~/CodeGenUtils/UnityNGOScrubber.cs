using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nomnom.CodeGenUtils {
    public static class UnityNGOScrubber {
        // ? most of this is from
        // ? https://github.com/EvaisaDev/LethalCompanyProjectCleaner/blob/482b32ce17525512a1103c1e59afa9b33da1f00b/LethalCompanyProjectCleaner/Program.cs#L235
        public static void Scrub(string[] files, Action<string> log) {
            foreach (var file in files) {
                if (!File.Exists(file)) {
                    // log($"[error] File \"{file}\" does not exist");
                    continue;
                }
                
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName == "UnitySourceGeneratedAssemblyMonoScriptTypes_v1") {
                    File.Delete(file);
                    continue;
                }
                
                var text = File.ReadAllText(file);
                if (fileName == "NfgoClient") {
                    // go to line that is public NfgoClient([NotNull] NfgoCommsNetwork network)
                    var index = text.IndexOf("public NfgoClient([NotNull] NfgoCommsNetwork network)", StringComparison.Ordinal);
                    // does it contain base(network)
                    if (text.IndexOf("base(network)", index, StringComparison.Ordinal) == -1) {
                        // add base(network)
                        var indexOfLastParenthesis = text.IndexOf(')', index);
                        text = text.Insert(indexOfLastParenthesis + 1, " : base(network)");
                    }
                }
                
                var tree = CSharpSyntaxTree.ParseText(text);
                var root = tree.GetRoot();
                var methodsToRemove = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Identifier.Text.StartsWith("__getTypeName") || m.Identifier.Text.StartsWith("__initializeVariables") ||
                                m.Identifier.Text.StartsWith("InitializeRPCS_") || m.Identifier.Text.StartsWith("__rpc_handler_"));

                var newRoot = root.RemoveNodes(methodsToRemove, SyntaxRemoveOptions.KeepNoTrivia);
                var classes = newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classes) {
                    // check if the class implements INetworkSerializable or IAsyncStateMachine
                    var implementedInterfaces = classDeclaration.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Select(x => x.Identifier.Text);

                    var newClassDeclaration = classDeclaration;

                    if (implementedInterfaces.Contains("INetworkSerializable")) {
                        // check if the class has the NetworkSerialize method
                        var hasNetworkSerialize = classDeclaration.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Any(x => x.Identifier.Text == "NetworkSerialize");

                        if (!hasNetworkSerialize) {
                            // add the NetworkSerialize method
                            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "NetworkSerialize")
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                .WithTypeParameterList(
                                    SyntaxFactory.TypeParameterList(SyntaxFactory.SingletonSeparatedList<TypeParameterSyntax>(SyntaxFactory.TypeParameter("T"))))
                                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier("serializer")).WithType(SyntaxFactory.IdentifierName("BufferSerializer<T>")))))
                                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ThrowStatement(SyntaxFactory
                                    .ObjectCreationExpression(SyntaxFactory.IdentifierName("System.NotImplementedException"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                            method = method.AddConstraintClauses(SyntaxFactory.TypeParameterConstraintClause("T")
                                .WithConstraints(
                                    SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                                        SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName("IReaderWriter")))));

                            newClassDeclaration = newClassDeclaration.AddMembers(method);
                        }
                    }

                    if (implementedInterfaces.Contains("IAsyncStateMachine")) {
                        // Check if the class has the MoveNext method
                        var hasMoveNext = classDeclaration.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Any(x => x.Identifier.Text == "MoveNext");

                        if (!hasMoveNext) {
                            // Add the MoveNext method
                            var moveNextMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "MoveNext")
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ThrowStatement(SyntaxFactory
                                    .ObjectCreationExpression(SyntaxFactory.IdentifierName("System.NotImplementedException"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                            newClassDeclaration = newClassDeclaration.AddMembers(moveNextMethod);
                        }

                        // Check if the class has the SetStateMachine method
                        var hasSetStateMachine = classDeclaration.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Any(x => x.Identifier.Text == "SetStateMachine");

                        if (!hasSetStateMachine) {
                            // Add the SetStateMachine method
                            var setStateMachineMethod = SyntaxFactory
                                .MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "SetStateMachine")
                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory
                                    .Parameter(SyntaxFactory.Identifier("stateMachine")).WithType(SyntaxFactory.IdentifierName("IAsyncStateMachine")))))
                                .WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ThrowStatement(SyntaxFactory
                                    .ObjectCreationExpression(SyntaxFactory.IdentifierName("System.NotImplementedException"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList())))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

                            newClassDeclaration = newClassDeclaration.AddMembers(setStateMachineMethod);
                        }
                    }

                    newRoot = newRoot.ReplaceNode(classDeclaration, newClassDeclaration);
                }

                if (newRoot != null) {
                    var rewriter = new RemoveCtorMethodCalls();
                    newRoot = rewriter.Visit(newRoot);
                }

                var structs = newRoot.DescendantNodes()
                    .OfType<StructDeclarationSyntax>()
                    .ToList(); // Collect all struct declarations into a list

                while (structs.Any(x => x.DescendantNodes().OfType<ConstructorDeclarationSyntax>().ToList().Count > 0)) {
                    foreach (var structDeclaration in structs) {
                        var constructors = structDeclaration.DescendantNodes()
                            .OfType<ConstructorDeclarationSyntax>()
                            .ToList(); // Collect all constructor declarations into a list

                        var currentStruct = structDeclaration; // Keep track of the current struct

                        foreach (var constructor in constructors) {
                            var newStruct = currentStruct.RemoveNode(constructor, SyntaxRemoveOptions.KeepNoTrivia);
                            newRoot = newRoot.ReplaceNode(currentStruct, newStruct);

                            currentStruct = newStruct; // Update the current struct
                        }
                    }

                    structs = newRoot.DescendantNodes()
                        .OfType<StructDeclarationSyntax>()
                        .ToList();
                }
                
                var newCode = newRoot.ToFullString();

                // write the new code back to the file
                File.WriteAllText(file, newCode);
            }
        }

        private static SyntaxNode Scrub__rpcCalls(SyntaxNode root) {
            var methodsToRemove = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text.StartsWith("__getTypeName") || m.Identifier.Text.StartsWith("__initializeVariables") ||
                            m.Identifier.Text.StartsWith("InitializeRPCS_") || m.Identifier.Text.StartsWith("__rpc_handler_"));
            var newRoot = root.RemoveNodes(methodsToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            return newRoot;
        }

        public static void ScrubDecompiledScript(string[] files, bool outputCopy, Action<string> log) {
            foreach (var file in files) {
                try {
                    // delete the copy file if it exists
                    var copyFile = file.Replace(".cs", ".copy.cs");
                    if (File.Exists(copyFile)) {
                        File.Delete(copyFile);
                    }

                    if (!File.Exists(file)) {
                        // log($"[error] File \"{file}\" does not exist");
                        continue;
                    }

                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName == "UnitySourceGeneratedAssemblyMonoScriptTypes_v1") {
                        File.Delete(file);
                        continue;
                    }

                    var text = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(text);
                    var root = tree.GetRoot();
                    root = Scrub__rpcCalls(root);

                    var methods = root.DescendantNodes().OfType<MemberDeclarationSyntax>().ToArray();
                    var methodsToReplace = new List<(MethodDeclarationSyntax, MethodDeclarationSyntax)>();
                    var nodesToRemove = new List<SyntaxNode>();

                    var bannedAttributes = new string[] {
                        "MonoPInvokeCallback"
                    };
                    
                    foreach (var method in methods) {
                        if (method is MethodDeclarationSyntax methodDeclaration) {
                            // log($"[info] - method name: {methodDeclaration.Identifier.Text}");
                            var attributes = methodDeclaration.AttributeLists
                                .SelectMany(x => x.Attributes)
                                .ToArray();

                            if (attributes.Any(x => bannedAttributes.Contains(x.Name.ToString()))) {
                                nodesToRemove.AddRange(attributes);
                                nodesToRemove.Add(methodDeclaration);
                                continue;
                            }
                            
                            var serverRpcAttribute = attributes
                                .FirstOrDefault(x => x.Name.ToString() == "ServerRpc");
                            var clientRpcAttribute = attributes
                                .FirstOrDefault(x => x.Name.ToString() == "ClientRpc");

                            MethodDeclarationSyntax? newMethod = null;
                            if (serverRpcAttribute != null) {
                                // check if serverRpcAttribute has RequireOwnership = false
                                // var hasRequireOwnershipFlag = serverRpcAttribute.ArgumentList?.Arguments
                                //     .Any(x => x.NameEquals?.Name.ToString() == "RequireOwnership" && x.Expression.ToString() == "false") == true;

                                newMethod = HandleRpcFunction(methodDeclaration, log);

                                // if (hasRequireOwnershipFlag) {
                                //     //newMethod = HandleBranchedRpc(file, methodDeclaration, false, log);
                                // } else {
                                //     //newMethod = HandleServerRpc(file, methodDeclaration, log);
                                // }
                            } else if (clientRpcAttribute != null) {
                                newMethod = HandleRpcFunction(methodDeclaration, log);
                                // newMethod = HandleClientRpc(file, methodDeclaration, log);
                            } else {
                                // log("unknown function");
                                continue;
                            }

                            if (newMethod != null) {
                                // log($"[info] new method: {newMethod}");

                                // replace old method with new method
                                // root = root.ReplaceNode(methodDeclaration, newMethod);
                                methodsToReplace.Add((methodDeclaration, newMethod));
                            }
                        }
                    }

                    // replace old methods with new methods
                    root = root.ReplaceNodes(methodsToReplace.Select(x => x.Item1), (x, y) => methodsToReplace.First(z => z.Item1 == x).Item2);
                    root = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

                    // write the new code back to the file
                    var newCode = root.ToFullString();

                    //todo: remove this from the generic code
                    var startOfRoundClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(x => x.Identifier.Text == "StartOfRound");
                    if (startOfRoundClass != null) {
                        // log($"[class] {startOfRoundClass.Identifier.Text}");
                        // root = root.ReplaceNode(startOfRoundClass, CleanStartOfRound(startOfRoundClass, log));
                        newCode = newCode.Replace(
                            @"voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.playerInput.actions.FindAction(""VoiceButton"").IsPressed() && !GameNetworkManager.Instance.localPlayerController.speakingToWalkieTalkie;",
                            @"// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.playerInput.actions.FindAction(""VoiceButton"").IsPressed() && !GameNetworkManager.Instance.localPlayerController.speakingToWalkieTalkie;"
                        );

                        newCode = newCode.Replace(
                            @"voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.settings.micEnabled;",
                            @"// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.settings.micEnabled;"
                        );

                        newCode = newCode.Replace(
                            @"if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isPlayerDead || voiceChatModule.IsMuted || !voiceChatModule.enabled || voiceChatModule == null)",
                            @"if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isPlayerDead || voiceChatModule == null)"
                        );

                        newCode = newCode.Replace(
                            @"allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();",
                            @"// allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();"
                        );

                        newCode = newCode.Replace(
                            @"playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();",
                            @"// playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();"
                        );
                    }

                    // write to copy file
                    File.WriteAllText(outputCopy ? copyFile : file, newCode);
                    // File.WriteAllText(file, newCode);
                } catch (Exception e) {
                    log($"[error] {e}");
                }
            }
        }

        private static MethodDeclarationSyntax? HandleRpcFunction(MethodDeclarationSyntax methodDeclaration, Action<string> log) {
            var statements = methodDeclaration.Body?.Statements;
            if (statements is not { } validStatements) {
                log($"[error] has no statements");
                return null;
            }

            /*
             * Example:
             * 		NetworkManager networkManager = base.NetworkManager;
		     *      if ((object)networkManager != null && networkManager.IsListening)
		     *      {
             */
            if (validStatements.Count == 2) {
                var secondStatement = validStatements[1];
                if (secondStatement is not IfStatementSyntax secondStatementIf) {
                    log($"[error] no secondStatementIf");
                    return null;
                }

                var childNodes = secondStatementIf.ChildNodes().ToArray();
                // foreach (var child in childNodes) {
                //     log($"[child] is {child.GetType().FullName}: {child}");
                // }

                if (childNodes.Length > 1) {
                    var secondChildNode = childNodes[1];
                    childNodes = secondChildNode.ChildNodes().ToArray();
                    // foreach (var child in childNodes) {
                    //     log($"- [child] is {child.GetType().FullName}: {child}");
                    // }

                    var nestedNode = childNodes.Length == 1 ? childNodes[0] : childNodes[1];
                    if (nestedNode is not IfStatementSyntax nestedNodeIf) {
                        log($"[error] no nestedNodeIf");
                        return null;
                    }
                
                    // strip this if statement of the prefix info and keep the rest
                    var strippedIfStatement = StripIfStatement(nestedNodeIf, log);
                    if (strippedIfStatement != null) {
                        var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                            .WithModifiers(methodDeclaration.Modifiers)
                            .WithParameterList(methodDeclaration.ParameterList)
                            .WithAttributeLists(methodDeclaration.AttributeLists)
                            .WithBody(SyntaxFactory.Block(strippedIfStatement));
                    
                        return newMethod;
                    } else {
                        var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                            .WithModifiers(methodDeclaration.Modifiers)
                            .WithParameterList(methodDeclaration.ParameterList)
                            .WithAttributeLists(methodDeclaration.AttributeLists)
                            .WithBody(nestedNodeIf.Statement as BlockSyntax);
                    
                        return newMethod;
                    }
                } else {
                    var thirdStatement = validStatements[3];
                    if (thirdStatement is not IfStatementSyntax thirdStatementIf) {
                        log($"[error] no thirdStatementIf");
                        return null;
                    }
                
                    // strip this if statement of the prefix info and keep the rest
                    var strippedIfStatement = StripIfStatement(thirdStatementIf, log);
                    log("<color=red>[error] not handled yet</color>");
                }
            } 
            /*
             * Example:
             * 		NetworkManager networkManager = base.NetworkManager;
		     *      if ((object)networkManager == null || !networkManager.IsListening)
		     *      {
			 *          return;
		     *      }
		     *      if (__rpc_exec_stage != __RpcExecStage.Client && (networkManager.IsServer || networkManager.IsHost))
		     *      {
			 *          ClientRpcParams clientRpcParams = default(ClientRpcParams);
			 *          FastBufferWriter bufferWriter = __beginSendClientRpc(848048148u, clientRpcParams, RpcDelivery.Reliable);
			 *          bufferWriter.WriteValueSafe(in setBool, default(FastBufferWriter.ForPrimitives));
			 *          bufferWriter.WriteValueSafe(in playSecondaryAudios, default(FastBufferWriter.ForPrimitives));
			 *          BytePacker.WriteValueBitPacked(bufferWriter, playerWhoTriggered);
			 *          __endSendClientRpc(ref bufferWriter, 848048148u, clientRpcParams, RpcDelivery.Reliable);
		     *      }
		     *      if (__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || GameNetworkManager.Instance.localPlayerController == null || (playerWhoTriggered != -1 && (int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerWhoTriggered))
             */
            else {
                var fourthStatement = validStatements[3];
                if (fourthStatement is not IfStatementSyntax fourthStatementIf) {
                    log($"[error] no fourthStatementIf");
                    return null;
                }
                
                var strippedIfStatement = StripIfStatement(fourthStatementIf, log);
                if (strippedIfStatement != null) {
                    var remainingStatements = validStatements.Skip(4);
                    var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                        .WithModifiers(methodDeclaration.Modifiers)
                        .WithParameterList(methodDeclaration.ParameterList)
                        .WithAttributeLists(methodDeclaration.AttributeLists)
                        .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements.Prepend(strippedIfStatement))));
                    
                    return newMethod;
                } else {
                    var remainingStatements = validStatements.Skip(4).ToArray();
                    
                    // if is empty
                    if (fourthStatementIf.Statement.ChildNodes().FirstOrDefault() is ReturnStatementSyntax) {
                        var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                            .WithModifiers(methodDeclaration.Modifiers)
                            .WithParameterList(methodDeclaration.ParameterList)
                            .WithAttributeLists(methodDeclaration.AttributeLists)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements)));

                        return newMethod;
                    } else {
                        var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                            .WithModifiers(methodDeclaration.Modifiers)
                            .WithParameterList(methodDeclaration.ParameterList)
                            .WithAttributeLists(methodDeclaration.AttributeLists)
                            .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements).Prepend(fourthStatementIf.Statement)));

                        return newMethod;
                    }
                    
                    // if (remainingStatements.Length > 0 && fourthStatementIf.Statement is ReturnStatementSyntax) {
                    //     var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    //         .WithModifiers(methodDeclaration.Modifiers)
                    //         .WithParameterList(methodDeclaration.ParameterList)
                    //         .WithAttributeLists(methodDeclaration.AttributeLists)
                    //         .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements)));
                    //     return newMethod;
                    // } else {
                    //     var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                    //         .WithModifiers(methodDeclaration.Modifiers)
                    //         .WithParameterList(methodDeclaration.ParameterList)
                    //         .WithAttributeLists(methodDeclaration.AttributeLists)
                    //         .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements).Prepend(fourthStatementIf.Statement)));
                    //     
                    //     return newMethod;
                    // }
                }
            }

            return null;
        }

        private static IfStatementSyntax? StripIfStatement(IfStatementSyntax ifStatementSyntax, Action<string> log) {
            var conditionString = ifStatementSyntax.Condition.ToString();
            // log($"[from] \"{conditionString}\"");
            foreach (var c in StripConditions) {
                conditionString = conditionString.Replace(c, string.Empty).TrimStart();
            }

            // log($"[to1] \"{conditionString}\"");

            if (string.IsNullOrEmpty(conditionString)) {
                // empty if statement
                // log("empty if statement!");
                return null;
            } else if (conditionString.StartsWith("||") || conditionString.StartsWith("&&")) {
                conditionString = conditionString[2..].TrimStart();
                ifStatementSyntax = SyntaxFactory.IfStatement(SyntaxFactory.ParseExpression(conditionString), ifStatementSyntax.Statement);
            }
            
            // log($"[to2] \"{conditionString}\"");
            
            return ifStatementSyntax;
        }

        private static MethodDeclarationSyntax? HandleServerRpc(string file, MethodDeclarationSyntax methodDeclaration, Action<string> log) {
            var statements = methodDeclaration.Body?.Statements;
            if (statements is not { } validStatements) {
                log($"[error] ServerRpc method {methodDeclaration.Identifier.Text} in {file} has no statements");
                return null;
            }

            // if (validStatements.Count < 4) {
            //     log($"[error] ServerRpc method {methodDeclaration.Identifier.Text} in {file} has less than 4 statements");
            //     return null;
            // }

            // var fourthStatement = validStatements[3];
            // if (fourthStatement is IfStatementSyntax ifStatementSyntax) {
            //     var condition = ifStatementSyntax.Condition.ToString();
            //     if (condition.Contains("__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost)")) {
            //         return HandleServerRpc_1(file, methodDeclaration, ifStatementSyntax, log);
            //     } else if (condition.Contains("__rpc_exec_stage != __RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost)")) {
            //         return HandleServerRpc_2(file, validStatements, methodDeclaration, ifStatementSyntax, log);
            //     } else {
            //         log($"[info] [ServerRpc3] found in {file}");
            //         return null;
            //     }
            // } else {
            //     log($"[info] [ServerRpc(RequireOwnership = false)] found in {file}");
            // }

            // return null;
            return HandleBranchedRpc(file, methodDeclaration, false, log);
        }

        private static MethodDeclarationSyntax HandleServerRpc_1(string file, MethodDeclarationSyntax methodDeclaration, IfStatementSyntax ifStatementSyntax, Action<string> log) {
            // HandleServerRpc(statements, log);
            log($"[info] [ServerRpc1] found in {file}");

            // get contents of statement
            var contents = ifStatementSyntax.Statement;

            // only use this statement
            var newStatements = SyntaxFactory.List(new[] { contents });

            // create new method
            var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                .WithModifiers(methodDeclaration.Modifiers)
                .WithParameterList(methodDeclaration.ParameterList)
                .WithAttributeLists(methodDeclaration.AttributeLists)
                .WithBody(SyntaxFactory.Block(newStatements));
            
            log($"[info] new method: {newMethod}");
            return newMethod;
        }
        
        private static MethodDeclarationSyntax HandleServerRpc_2(string file, SyntaxList<StatementSyntax> validStatements, MethodDeclarationSyntax methodDeclaration, IfStatementSyntax ifStatementSyntax, Action<string> log) {
            log($"[info] [ServerRpc2] found in {file}");
            
            // get the rest of the statements
            var restOfStatements = validStatements.Skip(4).ToList();
            // foreach (var statement in restOfStatements) {
            //     log($"[info] [ServerRpc2] rest of statements: {statement}");
            // }

            var newIfStatement = StripIfStatementCondition(ifStatementSyntax, false, log);
            var newStatements = restOfStatements.Prepend(newIfStatement).Where(x => x != null).Cast<StatementSyntax>().ToArray();
            var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                .WithModifiers(methodDeclaration.Modifiers)
                .WithParameterList(methodDeclaration.ParameterList)
                .WithAttributeLists(methodDeclaration.AttributeLists)
                .WithBody(SyntaxFactory.Block(newStatements));
            
            log($"[info] new method: {newMethod}");
            return newMethod;
        }

        private static MethodDeclarationSyntax? HandleBranchedRpc(string file, MethodDeclarationSyntax methodDeclaration, bool isClientRpc, Action<string> log) {
            var statements = methodDeclaration.Body?.Statements;
            if (statements is not { } validStatements) {
                log($"[error] Rpc method {methodDeclaration.Identifier.Text} in {file} has no statements");
                return null;
            }
            
            foreach (var statement in validStatements) {
                log($"[info] Rpc statement is {statement.GetType().FullName}: {statement.ToFullString()}");
            }
            
            // there are two situations, all nested within one if statement
            // or split into four statements

            StatementSyntax[] remainingStatements;
            if (validStatements.Count > 2) {
                // firstStatement = validStatements[3];
                remainingStatements = validStatements.Skip(3).ToArray();

                var ifStatement = (IfStatementSyntax)remainingStatements[0];
                // log($"[condition] {ifStatement.ToString()}");
                
                // first will be the final rpc statement, grab the remaining conditions
                var newIfStatement = StripIfStatementCondition(ifStatement, isClientRpc, log);
                if (newIfStatement == null) {
                    if (newIfStatement?.Statement.ChildNodes().Count() == 1 && newIfStatement.Statement.ChildNodes().First() is ReturnStatementSyntax) {
                        remainingStatements = remainingStatements[1..];
                    } else {
                        remainingStatements[0] = ifStatement.Statement;
                    }
                } else {
                    remainingStatements[0] = newIfStatement;
                }
            } else {
                log("nested");
                var firstStatement = (IfStatementSyntax)validStatements[1];    
                var newIfStatement = StripIfStatementCondition(firstStatement, isClientRpc, log);
                if (newIfStatement == null) {
                    firstStatement = (IfStatementSyntax)firstStatement.ChildNodes().ElementAt(1);
                    remainingStatements = new[] { firstStatement.Statement };
                } else {
                    remainingStatements = new[] { newIfStatement };
                }
                
                // var body = newIfStatement.Statement;
                // var childNodes = body.ChildNodes().ToArray();
                //
                // if (childNodes.Length < 2) {
                //     log($"[error] Rpc method {methodDeclaration.Identifier.Text} in {file} has less than 2 statements");
                //     return null;
                // }
                //
                // firstStatement = ((IfStatementSyntax)childNodes[1]).Statement;
                // remainingStatements = new[] { firstStatement };
            }
            
            // var statementType = validStatements.Count > 2 ? "// Multiple statements" : "// Nested if statement";
            // if (remainingStatements.Length != 0) {
            //     remainingStatements[0] = remainingStatements[0].WithLeadingTrivia(SyntaxFactory.Comment(statementType), SyntaxFactory.CarriageReturnLineFeed)
            //         .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            //     foreach (var statement in remainingStatements) {
            //         foreach (var node in statement.ChildNodes()) {
            //             log($"[info] Rpc final body node is {node.GetType().FullName}: {node.ToFullString()}");
            //         }
            //     }
            // } else {
            //     log($"[info] Rpc final body is empty");
            // }

            // create new method
            var newMethod = SyntaxFactory.MethodDeclaration(methodDeclaration.ReturnType, methodDeclaration.Identifier)
                .WithModifiers(methodDeclaration.Modifiers)
                .WithParameterList(methodDeclaration.ParameterList)
                .WithAttributeLists(methodDeclaration.AttributeLists)
                .WithBody(SyntaxFactory.Block(SyntaxFactory.List(remainingStatements)));
            
            log($"[info] new method: {newMethod}");
            return newMethod;
        }

        private static MethodDeclarationSyntax? HandleClientRpc(string file, MethodDeclarationSyntax methodDeclaration, Action<string> log) {
            var statements = methodDeclaration.Body?.Statements;
            if (statements is not { } validStatements) {
                log($"[error] ClientRpc method {methodDeclaration.Identifier.Text} in {file} has no statements");
                return null;
            }

            if (validStatements.Count < 2) {
                log($"[error] ClientRpc method {methodDeclaration.Identifier.Text} in {file} has less than 2 statements");
                return null;
            }

            // IfStatementSyntax ifStatement;
            // if (validStatements.Count == 2) {
            //     ifStatement = (IfStatementSyntax)validStatements[1];
            // } else {
            //     ifStatement = (IfStatementSyntax)validStatements[3];
            // }
            
            // var condition = ifStatement.Condition.ToString();
            return HandleBranchedRpc(file, methodDeclaration, true, log);
            // if (condition.Contains(
            //         "__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || NetworkManager.Singleton == null")) {
            //     return HandleBranchedRpc(file,
            //         methodDeclaration,
            //         true,
            //         log);
            // } else if (condition.Contains("__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost)")) {
            //     return HandleBranchedRpc(file,
            //         methodDeclaration,
            //         true,
            //         log);
            // } else {
            //     return HandleBranchedRpc(file,
            //         methodDeclaration,
            //         null,
            //         true,
            //         log);
            // }
        }

        private readonly static string[] StripConditions = {
            "__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost)",
            "__rpc_exec_stage != __RpcExecStage.Client || (!networkManager.IsClient && !networkManager.IsHost) || NetworkManager.Singleton == null",
            "__rpc_exec_stage == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost)",
            "__rpc_exec_stage != __RpcExecStage.Server || (!networkManager.IsServer && !networkManager.IsHost)",
            "__rpc_exec_stage == __RpcExecStage.Server && (networkManager.IsServer || networkManager.IsHost)",
            "__rpc_exec_stage == __RpcExecStage.Client && !networkManager.IsClient && networkManager.IsHost"
        };

        private static IfStatementSyntax? StripIfStatementCondition(IfStatementSyntax ifStatementSyntax, bool keepContent, Action<string> log) {
            var actualCondition = ifStatementSyntax.Condition;
            var conditionString = actualCondition.ToString();

            foreach (var c in StripConditions) {
                conditionString = conditionString.Replace(c, string.Empty).TrimStart();
            }
            
            log($"[new condition1] \"{conditionString}\"");

            if (!string.IsNullOrEmpty(conditionString) && (conditionString.StartsWith("||") || conditionString.StartsWith("&&"))) {
                conditionString = conditionString[2..].TrimStart();
            }
            
            log($"[new condition2] \"{conditionString}\"");
            
            var newIfStatement = SyntaxFactory.IfStatement(
                SyntaxFactory.ParseExpression(conditionString),
                keepContent ? ifStatementSyntax.Statement : SyntaxFactory.Block(SyntaxFactory.ReturnStatement())
            );

            if (string.IsNullOrEmpty(newIfStatement.Condition.ToString())) {
                return null;
            }

            return newIfStatement;
        }

        // private static ClassDeclarationSyntax CleanStartOfRound(ClassDeclarationSyntax startOfRoundClass, Action<string> log) {
        //     log($"[class] {startOfRoundClass.Identifier.Text}");
        //     
        //     // get update function
        //     var methods = startOfRoundClass.DescendantNodes().OfType<MemberDeclarationSyntax>().ToArray();
        //     var updateFunction = methods.Where(x => x is MethodDeclarationSyntax).Cast<MethodDeclarationSyntax>().First(x => x.Identifier.Text == "Update");
        //     
        //     log($"[update] {updateFunction}");
        //     
        //     // foreach (var method in methods) {
        //     //     log($"[method] {method}");
        //     // }
        //     
        //     // go through all the lines in the function and comment out any that start with voiceChatModule
        //     // all lines recursively
        //     var newStatements = new List<StatementSyntax>();
        //     foreach (var statement in updateFunction.Body!.Statements) {
        //         if (statement is ExpressionStatementSyntax expressionStatement) {
        //             var expression = expressionStatement.Expression;
        //             if (expression is InvocationExpressionSyntax invocationExpression) {
        //                 var expressionString = invocationExpression.Expression.ToString();
        //                 if (expressionString.StartsWith("voiceChatModule")) {
        //                     newStatements.Add(SyntaxFactory.ExpressionStatement(SyntaxFactory.ParseExpression($"// {expressionStatement}")));
        //                 } else {
        //                     newStatements.Add(expressionStatement);
        //                 }
        //             } else {
        //                 newStatements.Add(expressionStatement);
        //             }
        //         } else {
        //             newStatements.Add(statement);
        //         }
        //     }
        //     
        //     // create new update function
        //     var newUpdateFunction = updateFunction.WithBody(SyntaxFactory.Block(newStatements));
        //     
        //     // replace old update function with new update function
        //     var newStartOfRoundClass = startOfRoundClass.ReplaceNode(updateFunction, newUpdateFunction);
        //     return newStartOfRoundClass;
        // }
    }

    public class RemoveCtorMethodCalls : CSharpSyntaxRewriter {
        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node) {
            if (node.Expression is InvocationExpressionSyntax invocation &&
                invocation.Expression.ToString().Contains("ctor")) {
                // If the expression is a method call containing "ctor", remove it.
                return null;
            } else {
                // Otherwise, keep the original node.
                return base.VisitExpressionStatement(node);
            }
        }
    }
}
