using System.Text.Json.Serialization;

namespace Microsoft.Azure.Databricks.Client.Models
{
    public class RunNowSettings : RunParameters
    {
        /// <summary>
        /// The canonical identifier for this job.
        /// </summary>
        [JsonPropertyName("job_id")]
        public long JobId { get; set; }
    }
}