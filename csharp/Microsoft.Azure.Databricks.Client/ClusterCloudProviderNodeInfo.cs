using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Databricks.Client
{
    public record ClusterCloudProviderNodeInfo
    {
        /// <summary>
        /// Available CPU core quota.
        /// </summary>
        [JsonPropertyName("available_core_quota")]
        public int AvailableCoreQuota { get; set; }

        /// <summary>
        /// Total CPU core quota.
        /// </summary>
        [JsonPropertyName("total_core_quota")]
        public int TotalCoreQuota { get; set; }

        /// <summary>
        /// Status as reported by the cloud provider.
        /// </summary>
        [JsonPropertyName("status")]
        public IEnumerable<ClusterCloudProviderNodeStatus> Status { get; set; }
    }
}