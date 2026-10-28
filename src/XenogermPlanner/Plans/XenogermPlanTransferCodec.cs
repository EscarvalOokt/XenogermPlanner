using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Verse;

namespace XenogermPlanner.Plans
{
    internal enum XenogermPlanTransferFailure
    {
        None,
        MalformedPayload,
        UnsupportedVersion
    }

    internal static class XenogermPlanTransferCodec
    {
        private const string Marker = "XENOGERM_PLANNER_PLAN";
        private const int CurrentVersion = 1;
        private const string VersionPrefix = "version=";
        private const string NamePrefix = "name=";
        private const string ReadinessModePrefix = "readinessMode=";
        private const string GenePrefix = "gene=";
        private const string CoverageToken = "Coverage";
        private const string ExactPayloadToken = "ExactPayload";

        private static readonly Encoding _strictUtf8 = new UTF8Encoding(false, true);

        internal static string Serialize(XenogermPlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            var geneDefNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (GeneDef gene in plan.DesiredGenes)
            {
                if (gene == null || string.IsNullOrWhiteSpace(gene.defName))
                {
                    throw new InvalidOperationException("Plan contains a gene without a valid def name.");
                }

                geneDefNames.Add(gene.defName);
            }

            foreach (string geneDefName in plan.UnresolvedDesiredGeneDefNames)
            {
                if (string.IsNullOrWhiteSpace(geneDefName))
                {
                    throw new InvalidOperationException("Plan contains an unresolved gene without a valid def name.");
                }

                geneDefNames.Add(geneDefName);
            }

            var lines = new List<string>
            {
                Marker,
                VersionPrefix + CurrentVersion.ToString(CultureInfo.InvariantCulture),
                NamePrefix + Encode(plan.Name),
                ReadinessModePrefix + GetReadinessModeToken(plan.ReadinessMode)
            };

            foreach (string geneDefName in geneDefNames.OrderBy(name => name, StringComparer.Ordinal))
                lines.Add(GenePrefix + Encode(geneDefName));

            return string.Join("\n", lines);
        }

        internal static bool TryDeserialize(
            string payload,
            Func<string, GeneDef> resolveGene,
            out XenogermPlan plan,
            out XenogermPlanTransferFailure failure)
        {
            if (resolveGene == null)
                throw new ArgumentNullException(nameof(resolveGene));

            plan = null;
            failure = XenogermPlanTransferFailure.None;

            List<string> lines = ReadLines(payload);

            if (lines == null || lines.Count < 2 || !string.Equals(lines[0], Marker, StringComparison.Ordinal))
            {
                failure = XenogermPlanTransferFailure.MalformedPayload;

                return false;
            }

            if (!TryReadVersion(lines[1], out int version))
            {
                failure = XenogermPlanTransferFailure.MalformedPayload;

                return false;
            }

            if (version != CurrentVersion)
            {
                failure = XenogermPlanTransferFailure.UnsupportedVersion;

                return false;
            }

            string name = null;
            var readinessMode = default(PlanReadinessMode);
            var hasName = false;
            var hasReadinessMode = false;
            var geneDefNames = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 2; index < lines.Count; index++)
            {
                string line = lines[index];

                if (line.StartsWith(NamePrefix, StringComparison.Ordinal))
                {
                    if (hasName || !TryDecode(line.Substring(NamePrefix.Length), out name))
                    {
                        failure = XenogermPlanTransferFailure.MalformedPayload;

                        return false;
                    }

                    hasName = true;

                    continue;
                }

                if (line.StartsWith(ReadinessModePrefix, StringComparison.Ordinal))
                {
                    if (hasReadinessMode ||
                        !TryParseReadinessMode(line.Substring(ReadinessModePrefix.Length), out readinessMode))
                    {
                        failure = XenogermPlanTransferFailure.MalformedPayload;

                        return false;
                    }

                    hasReadinessMode = true;

                    continue;
                }

                if (line.StartsWith(GenePrefix, StringComparison.Ordinal))
                {
                    if (!TryDecode(line.Substring(GenePrefix.Length), out string geneDefName) ||
                        string.IsNullOrWhiteSpace(geneDefName))
                    {
                        failure = XenogermPlanTransferFailure.MalformedPayload;

                        return false;
                    }

                    geneDefNames.Add(geneDefName);

                    continue;
                }

                failure = XenogermPlanTransferFailure.MalformedPayload;

                return false;
            }

            if (!hasName ||
                !hasReadinessMode ||
                !XenogermPlanNameAllocator.TryNormalize(name, out string normalizedName))
            {
                failure = XenogermPlanTransferFailure.MalformedPayload;

                return false;
            }

            var desiredGenes = new List<GeneDef>();
            var unresolvedGeneDefNames = new List<string>();

            foreach (string geneDefName in geneDefNames.OrderBy(value => value, StringComparer.Ordinal))
            {
                GeneDef gene = resolveGene(geneDefName);

                if (gene == null)
                    unresolvedGeneDefNames.Add(geneDefName);
                else
                    desiredGenes.Add(gene);
            }

            plan = XenogermPlan.CreateIndependent(normalizedName, desiredGenes, unresolvedGeneDefNames, readinessMode);

            return true;
        }

        private static List<string> ReadLines(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return null;

            string normalizedPayload = payload.Replace("\r\n", "\n").Replace('\r', '\n');
            var lines = new List<string>(normalizedPayload.Split(['\n'], StringSplitOptions.None));

            while (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }

        private static bool TryReadVersion(string line, out int version)
        {
            version = 0;

            if (line == null || !line.StartsWith(VersionPrefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(
                line.Substring(VersionPrefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out version);
        }

        private static string GetReadinessModeToken(PlanReadinessMode readinessMode)
        {
            switch (readinessMode)
            {
                case PlanReadinessMode.Coverage:
                    return CoverageToken;

                case PlanReadinessMode.ExactPayload:
                    return ExactPayloadToken;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(readinessMode),
                        readinessMode,
                        "Unsupported plan readiness mode.");
            }
        }

        private static bool TryParseReadinessMode(string token, out PlanReadinessMode readinessMode)
        {
            if (string.Equals(token, CoverageToken, StringComparison.Ordinal))
            {
                readinessMode = PlanReadinessMode.Coverage;

                return true;
            }

            if (string.Equals(token, ExactPayloadToken, StringComparison.Ordinal))
            {
                readinessMode = PlanReadinessMode.ExactPayload;

                return true;
            }

            readinessMode = default;

            return false;
        }

        private static string Encode(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Convert.ToBase64String(_strictUtf8.GetBytes(value));
        }

        private static bool TryDecode(string value, out string decodedValue)
        {
            decodedValue = null;

            try
            {
                byte[] bytes = Convert.FromBase64String(value);

                if (!string.Equals(Convert.ToBase64String(bytes), value, StringComparison.Ordinal))
                    return false;

                decodedValue = _strictUtf8.GetString(bytes);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }
    }
}