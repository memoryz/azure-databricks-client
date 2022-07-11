using Microsoft.Azure.Databricks.Client.Models;
using System.Text.Json;

namespace Microsoft.Azure.Databricks.Client.Test
{
    [TestClass]
    public class JobApiClientTest: ApiClientTest
    {
        [TestMethod]
        public void TestSerialization()
        {
            string request = """
            {
              "name": "A multitask job",
              "tags": {
                "cost-center": "engineering",
                "team": "jobs"
              },
              "tasks": [
                {
                  "task_key": "Sessionize",
                  "description": "Extracts session data from events",
                  "depends_on": [],
                  "existing_cluster_id": "0923-164208-meows279",
                  "spark_jar_task": {
                    "main_class_name": "com.databricks.Sessionize",
                    "parameters": ["--data", "dbfs:/path/to/data.json"]
                  },
                  "libraries": [
                    {
                      "jar": "dbfs:/mnt/databricks/Sessionize.jar"
                    }
                  ],
                  "timeout_seconds": 86400,
                  "max_retries": 3,
                  "min_retry_interval_millis": 2000,
                  "retry_on_timeout": false
                },
                {
                  "task_key": "Orders_Ingest",
                  "description": "Ingests order data",
                  "depends_on": [],
                  "job_cluster_key": "auto_scaling_cluster",
                  "spark_jar_task": {
                    "main_class_name": "com.databricks.OrdersIngest",
                    "parameters": ["--data", "dbfs:/path/to/order-data.json"]
                  },
                  "libraries": [
                    {
                      "jar": "dbfs:/mnt/databricks/OrderIngest.jar"
                    }
                  ],
                  "timeout_seconds": 86400,
                  "max_retries": 3,
                  "min_retry_interval_millis": 2000,
                  "retry_on_timeout": false
                },
                {
                  "task_key": "Match",
                  "description": "Matches orders with user sessions",
                  "depends_on": [
                    {
                      "task_key": "Orders_Ingest"
                    },
                    {
                      "task_key": "Sessionize"
                    }
                  ],
                  "new_cluster": {
                    "spark_version": "7.3.x-scala2.12",
                    "node_type_id": "i3.xlarge",
                    "spark_conf": {
                      "spark.speculation": "true"
                    },
                    "aws_attributes": {
                      "availability": "SPOT",
                      "zone_id": "us-west-2a"
                    },
                    "autoscale": {
                      "min_workers": 2,
                      "max_workers": 16
                    }
                  },
                  "notebook_task": {
                    "notebook_path": "/Users/user.name@databricks.com/Match",
                    "base_parameters": {
                      "name": "John Doe",
                      "age": "35"
                    }
                  },
                  "timeout_seconds": 86400,
                  "max_retries": 3,
                  "min_retry_interval_millis": 2000,
                  "retry_on_timeout": false
                }
              ],
              "job_clusters": [
                {
                  "job_cluster_key": "auto_scaling_cluster",
                  "new_cluster": {
                    "spark_version": "7.3.x-scala2.12",
                    "node_type_id": "i3.xlarge",
                    "spark_conf": {
                      "spark.speculation": "true"
                    },
                    "aws_attributes": {
                      "availability": "SPOT",
                      "zone_id": "us-west-2a"
                    },
                    "autoscale": {
                      "min_workers": 2,
                      "max_workers": 16
                    }
                  }
                }
              ],
              "email_notifications": {
                "on_start": ["user.name@databricks.com"],
                "on_success": ["user.name@databricks.com"],
                "on_failure": ["user.name@databricks.com"],
                "no_alert_for_skipped_runs": false
              },
              "timeout_seconds": 86400,
              "schedule": {
                "quartz_cron_expression": "20 30 * * * ?",
                "timezone_id": "Europe/London",
                "pause_status": "PAUSED"
              },
              "max_concurrent_runs": 10,
              "git_source": null,
              "format": "MULTI_TASK",
              "access_control_list": [
                {
                  "user_name": "jsmith@example.com",
                  "permission_level": "CAN_MANAGE"
                },
                {
                  "group_name": "readonly-group@example.com",
                  "permission_level": "CAN_VIEW"
                }
              ]
            }
            """;
            
            JobSettings jobSettings = JsonSerializer.Deserialize<JobSettings>(request, options)!;
            Assert.IsNotNull(jobSettings.Tasks);
            Assert.IsTrue(jobSettings.Tasks.All(
                task => task.DependsOn.All(dep => dep is JobTaskSettings)
            ));
        }
    }
}
