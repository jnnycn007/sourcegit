using System;
using System.Collections.Generic;

namespace SourceGit.Models
{
    public enum GitFlowVersion
    {
        None = 0,
        Classic,
        Next,
    }

    public enum GitFlowBranchType
    {
        None = 0,
        Feature,
        Release,
        Hotfix,
    }

    public class GitFlow
    {
        public string ProductionBranch { get; set; } = string.Empty;
        public string DevelopmentBranch { get; set; } = string.Empty;
        public string FeaturePrefix { get; set; } = string.Empty;
        public string ReleasePrefix { get; set; } = string.Empty;
        public string HotfixPrefix { get; set; } = string.Empty;

        public void Parse(Dictionary<string, string> config, GitFlowVersion version)
        {
            if (version == GitFlowVersion.Classic)
            {
                if (config.TryGetValue("gitflow.branch.master", out var masterName))
                    ProductionBranch = masterName;
                if (config.TryGetValue("gitflow.branch.develop", out var developName))
                    DevelopmentBranch = developName;
                if (config.TryGetValue("gitflow.prefix.feature", out var featurePrefix))
                    FeaturePrefix = featurePrefix;
                if (config.TryGetValue("gitflow.prefix.release", out var releasePrefix))
                    ReleasePrefix = releasePrefix;
                if (config.TryGetValue("gitflow.prefix.hotfix", out var hotfixPrefix))
                    HotfixPrefix = hotfixPrefix;
            }
            else if (version == GitFlowVersion.Next)
            {
                foreach (var kv in config)
                {
                    if (!kv.Key.StartsWith("gitflow.branch.", StringComparison.Ordinal))
                        continue;

                    if (kv.Key.EndsWith(".type", StringComparison.Ordinal) && kv.Value.Equals("base", StringComparison.Ordinal))
                    {
                        var b = kv.Key.Substring("gitflow.branch.".Length, kv.Key.Length - "gitflow.branch.".Length - ".type".Length);
                        if (config.ContainsKey($"gitflow.branch.{b}.parent"))
                            DevelopmentBranch = b;
                        else
                            ProductionBranch = b;
                    }
                    else if (kv.Key.EndsWith(".prefix", StringComparison.Ordinal))
                    {
                        var t = kv.Key.Substring("gitflow.branch.".Length, kv.Key.Length - "gitflow.branch.".Length - ".prefix".Length);
                        if (t.Equals("feature", StringComparison.Ordinal))
                            FeaturePrefix = kv.Value;
                        else if (t.Equals("release", StringComparison.Ordinal))
                            ReleasePrefix = kv.Value;
                        else if (t.Equals("hotfix", StringComparison.Ordinal))
                            HotfixPrefix = kv.Value;
                    }
                }
            }
            else
            {
                ProductionBranch = string.Empty;
                DevelopmentBranch = string.Empty;
                FeaturePrefix = string.Empty;
                ReleasePrefix = string.Empty;
                HotfixPrefix = string.Empty;
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(ProductionBranch) &&
                    !string.IsNullOrEmpty(DevelopmentBranch) &&
                    !string.IsNullOrEmpty(FeaturePrefix) &&
                    !string.IsNullOrEmpty(ReleasePrefix) &&
                    !string.IsNullOrEmpty(HotfixPrefix);
            }
        }

        public string GetPrefix(GitFlowBranchType type)
        {
            return type switch
            {
                GitFlowBranchType.Feature => FeaturePrefix,
                GitFlowBranchType.Release => ReleasePrefix,
                GitFlowBranchType.Hotfix => HotfixPrefix,
                _ => string.Empty,
            };
        }
    }
}
