#if UNITY_2020_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Quickenshtein;
using UnityEngine.Pool;

namespace Nomnom.UnityProjectPatcher.UnityPackages {
    public static class PackagesUtility {
#if UNITY_EDITOR
        public static IEnumerable<FoundPackageInfo> GetGamePackages(UPPatcherSettings settings) {
            var ignoredPrefixes = settings.IgnoredDllPrefixes;
            var files = Directory.EnumerateFiles(settings.GameManagedPath!, "*.dll");
            var convertedFiles = files
                .Where(x => !ignoredPrefixes.Any(x.StartsWith))
                .Select(x =>
                {
                    var name = Path.GetFileNameWithoutExtension(x)
                        .ToLowerInvariant();
                    return (original: x, package: $"com.{name}");
                }).ToArray();

            UnityEditor.EditorUtility.DisplayProgressBar("Grabbing Packages", "Grabbing packages from the registry", 0);

            var packageRequest = UnityEditor.PackageManager.Client.SearchAll();
            while (!packageRequest.IsCompleted) {
            }

            UnityEditor.EditorUtility.ClearProgressBar();

            var packages = packageRequest.Result;
            using var _ = ListPool<UnityEditor.PackageManager.PackageInfo>.Get(out var exactMatches);
            using var __ = ListPool<(UnityEditor.PackageManager.PackageInfo, int)>.Get(out var possibleMatches);
            foreach (var package in packages) {
                if (convertedFiles.Any(x => x.package == package.name)) {
                    exactMatches.Add(package);
                    continue;
                }

                // some edge case detections
                if (package.name == "com.unity.netcode.gameobjects") {
                    if (convertedFiles.Any(x => x.package.Contains("gameobjects", StringComparison.InvariantCulture))) {
                        exactMatches.Add(package);
                        continue;
                    }
                }

                var closest = Enumerable.Select(
                        convertedFiles,
                        x => (x, getMinDistance(x.package, package.name))
                    )
                    .OrderBy(x => x.Item2)
                    .FirstOrDefault();

                if (closest.x.original is not null) {
                    if (closest.Item2 == 0) {
                        exactMatches.Add(package);
                        continue;
                    }

                    possibleMatches.Add((package, closest.Item2));
                }
            }

            const int maxDistance = 6;
            var matchesOverMaxDistance = possibleMatches
                .Where(x => x.Item2 > maxDistance)
                .ToArray();

            int getMinDistance(string dllPackageName, string sourcePackageName) {
                var d1 = Levenshtein.GetDistance(dllPackageName, sourcePackageName);
                var dllPackageNameWithoutLastTerm = dllPackageName[..(dllPackageName.LastIndexOf('.'))];
                var d2 = Levenshtein.GetDistance(dllPackageNameWithoutLastTerm, sourcePackageName);
                var sourcePackageNameNoDashes = sourcePackageName.Replace("-", string.Empty);
                var d3 = Levenshtein.GetDistance(dllPackageName, sourcePackageNameNoDashes);
                var d4 = Levenshtein.GetDistance(dllPackageNameWithoutLastTerm, sourcePackageNameNoDashes);
                return Mathf.Min(d1, d2, d3, d4);
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Found packages for {settings.GameName} @ {settings.GameVersion} (<i>click for details</i>):");
            sb.AppendLine();

            if (exactMatches.Count > 0) {
                sb.AppendLine($"<b>Found {exactMatches.Count} exact matches:</b>");
                foreach (var package in exactMatches) {
                    sb.AppendLine($" - {package.packageId} <color=#FFFFFF7F>(+{package.dependencies.Length} dependencies)</color>");

                    // foreach (var dependency in package.dependencies) {
                    //     sb.AppendLine($"   + <color=#FFFFFF7F>{dependency.name} @ {dependency.version}</color>");
                    // }
                }
            }

            // Debug.Log(sb.ToString());
            // sb.Clear();

            if (possibleMatches.Count > 0) {
                if (exactMatches.Count > 0) {
                    sb.AppendLine();
                }

                sb.AppendLine($"<b>Found {possibleMatches.Count - matchesOverMaxDistance.Length} possible matches:</b>");
                foreach (var package in possibleMatches.OrderBy(x => x.Item2).Except(matchesOverMaxDistance)) {
                    var t = package.Item2 / (float)maxDistance;
                    var finalT = Mathf.Clamp(1f - t, 0.2f, 1f);

                    var color = new Color(1, 1, 1, finalT);
                    var hex = ColorUtility.ToHtmlStringRGBA(color);

                    sb.AppendLine($" - <color=#{hex}> {package.Item1.packageId} ({Mathf.Clamp01(1f - t):P000})</color>");
                }

                if (matchesOverMaxDistance.Length > 0) {
                    sb.AppendLine();
                    sb.AppendLine($"<b>Found {matchesOverMaxDistance.Length} improbable matches:</b>");
                    foreach (var package in matchesOverMaxDistance.OrderBy(x => x.Item2)) {
                        var t = package.Item2 / (float)maxDistance;
                        sb.AppendLine($" - {package.Item1.packageId} ({Mathf.Clamp01(1f - t):P000})");
                    }
                }
            }

            Debug.Log(sb.ToString());

            foreach (var package in exactMatches) {
                yield return new FoundPackageInfo(package, PackageMatchType.Exact);
            }

            foreach (var package in possibleMatches.Except(matchesOverMaxDistance).Select(x => x.Item1)) {
                yield return new FoundPackageInfo(package, PackageMatchType.Possible);
            }

            foreach (var package in matchesOverMaxDistance.Select(x => x.Item1)) {
                yield return new FoundPackageInfo(package, PackageMatchType.Improbable);
            }
        }
#endif
    }
}
#endif