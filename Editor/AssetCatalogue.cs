using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Nomnom.UnityProjectPatcher.Editor {
    /// <summary>
    /// A catalogue of assets & associated metadata information.
    /// </summary>
    [Serializable]
    public class AssetCatalogue {
        public readonly string RootAssetsPath;
        public Entry[] Entries;

        public AssetCatalogue(string rootAssetsPath, Entry[] entries) {
            RootAssetsPath = rootAssetsPath;
            Entries = entries;
        }

        public AssetCatalogue(string rootAssetsPath, IEnumerable<Entry> entries) {
            RootAssetsPath = rootAssetsPath;
            Entries = entries.ToArray();
        }

        public void InsertFrom(AssetCatalogue other) {
            Array.Resize(ref Entries, Entries.Length + other.Entries.Length);
            Array.Copy(other.Entries, 0, Entries, Entries.Length - other.Entries.Length, other.Entries.Length);
        }

        public void Compare(AssetCatalogue other) {
            // pair by relative paths
            var pairs = Entries.Join(
                other.Entries,
                x => x.RelativePathToRoot,
                x => x.RelativePathToRoot,
                (a, b) => (a, b)
            ).ToList();
            pairs.Sort((x, y) => String.Compare(x.a.RelativePathToRoot, y.a.RelativePathToRoot, StringComparison.Ordinal));

            // print
            foreach (var pair in pairs) {
                Debug.Log($"{pair.a} <-> {pair.b}");
            }
        }

        public override string ToString() {
            return ToString(true);
        }

        public string ToString(bool withTags) {
            var sb = new System.Text.StringBuilder();
            if (withTags) {
                sb.AppendLine($"AssetCatalogue of {Entries.Length} entries <i>(click for details)</i>");
                sb.AppendLine($"<b>\"Assets\" root</b>: {RootAssetsPath}");
            } else {
                sb.AppendLine($"AssetCatalogue of {Entries.Length} entries (click for details)");
                sb.AppendLine($"\"Assets\" root");
            }
            sb.AppendLine();

            // group by folder
            var groups = Entries.GroupBy(x => Path.GetDirectoryName(x.RelativePathToRoot));

            // build tree
            foreach (var group in groups) {
                if (withTags) {
                    sb.AppendLine($"- <b>{(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar : group.Key)}</b>");
                } else {
                    sb.AppendLine($"- {(string.IsNullOrEmpty(group.Key) ? Path.DirectorySeparatorChar : group.Key)}");
                }

                foreach (var entry in group) {
                    sb.AppendLine($"  - {entry.ToString(withTags)}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public class Entry {
            private const string FileIdColor = "#c0c0c07f";

            public string RelativePathToRoot;
            public string Guid;
            public long? FileId;

            public Entry(string relativePathToRoot, string guid, long? fileId) {
                RelativePathToRoot = relativePathToRoot;
                Guid = guid;
                FileId = fileId;
            }

            public override string ToString() {
                return ToString(true);
            }

            public virtual string ToString(bool withTags) {
                var fileId = FileId?.ToString() ?? "n/a";
                if (withTags) {
                    return $"[{Guid}] <color={FileIdColor}>{fileId,19}</color> {RelativePathToRoot}";
                }

                return $"[{Guid}] {fileId,19} {RelativePathToRoot}";
            }
        }

        public class ScriptEntry : Entry {
            public string FullTypeName;

            public ScriptEntry(string relativePathToRoot, string guid, long? fileId, string fullTypeName) : base(relativePathToRoot, guid, fileId) {
                FullTypeName = fullTypeName;
            }

            public override string ToString() {
                return ToString(true);
            }

            public override string ToString(bool withTags) {
                return $"{base.ToString(withTags)} [{FullTypeName}]";
            }
        }
    }
}