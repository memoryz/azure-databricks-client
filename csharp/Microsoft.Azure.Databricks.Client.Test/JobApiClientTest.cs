using Microsoft.Azure.Databricks.Client.Models;
using Moq.Contrib.HttpClient;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Moq;

namespace Microsoft.Azure.Databricks.Client.Test;

[TestClass]
public class JobApiClientTest : ApiClientTest
{
    private static readonly Uri JobsApiUri = new(BaseApiUri, "2.1/jobs/");

    #region Test resource
    private const string MultiTaskJobJson = @"
            {
                ""name"": ""A multitask job"",
                ""tags"": {
                    ""cost-center"": ""engineering"",
                    ""team"": ""jobs""
                },
                ""tasks"": [{
                        ""task_key"": ""Sessionize"",
                        ""description"": ""Extracts session data from events"",
                        ""depends_on"": [],
                        ""existing_cluster_id"": ""0923-164208-meows279"",
                        ""spark_jar_task"": {
                            ""main_class_name"": ""com.databricks.Sessionize"",
                            ""parameters"": [""--data"", ""dbfs:/path/to/data.json""]
                        },
                        ""libraries"": [{
                            ""jar"": ""dbfs:/mnt/databricks/Sessionize.jar""
                        }],
                        ""timeout_seconds"": 86400,
                        ""max_retries"": 3,
                        ""min_retry_interval_millis"": 2000,
                        ""retry_on_timeout"": false
                    },
                    {
                        ""task_key"": ""Orders_Ingest"",
                        ""description"": ""Ingests order data"",
                        ""depends_on"": [],
                        ""job_cluster_key"": ""auto_scaling_cluster"",
                        ""spark_jar_task"": {
                            ""main_class_name"": ""com.databricks.OrdersIngest"",
                            ""parameters"": [""--data"", ""dbfs:/path/to/order-data.json""]
                        },
                        ""libraries"": [{
                            ""jar"": ""dbfs:/mnt/databricks/OrderIngest.jar""
                        }],
                        ""timeout_seconds"": 86400,
                        ""max_retries"": 3,
                        ""min_retry_interval_millis"": 2000,
                        ""retry_on_timeout"": false
                    },
                    {
                        ""task_key"": ""Match"",
                        ""description"": ""Matches orders with user sessions"",
                        ""depends_on"": [{
                                ""task_key"": ""Orders_Ingest""
                            },
                            {
                                ""task_key"": ""Sessionize""
                            }
                        ],
                        ""new_cluster"": {
                            ""spark_version"": ""7.3.x-scala2.12"",
                            ""node_type_id"": ""Standard_D3_v2"",
                            ""spark_conf"": {
                                ""spark.speculation"": ""true""
                            },
                            ""azure_attributes"": {
                                ""availability"": ""ON_DEMAND_AZURE"",
                                ""spot_bid_max_price"": -1,
                                ""first_on_demand"": 0
                            },
                            ""autoscale"": {
                                ""min_workers"": 2,
                                ""max_workers"": 16
                            }
                        },
                        ""notebook_task"": {
                            ""notebook_path"": ""/Users/user.name@databricks.com/Match"",
                            ""base_parameters"": {
                                ""name"": ""John Doe"",
                                ""age"": ""35""
                            }
                        },
                        ""timeout_seconds"": 86400,
                        ""max_retries"": 3,
                        ""min_retry_interval_millis"": 2000,
                        ""retry_on_timeout"": false
                    }
                ],
                ""job_clusters"": [{
                    ""job_cluster_key"": ""auto_scaling_cluster"",
                    ""new_cluster"": {
                        ""spark_version"": ""7.3.x-scala2.12"",
                        ""node_type_id"": ""Standard_D3_v2"",
                        ""spark_conf"": {
                            ""spark.speculation"": ""true""
                        },
                        ""azure_attributes"": {
                            ""availability"": ""ON_DEMAND_AZURE"",
                            ""spot_bid_max_price"": -1,
                            ""first_on_demand"": 0
                        },
                        ""autoscale"": {
                            ""min_workers"": 2,
                            ""max_workers"": 16
                        }
                    }
                }],
                ""email_notifications"": {
                    ""on_start"": [""user.name@databricks.com""],
                    ""on_success"": [""user.name@databricks.com""],
                    ""on_failure"": [""user.name@databricks.com""],
                    ""no_alert_for_skipped_runs"": false
                },
                ""timeout_seconds"": 86400,
                ""schedule"": {
                    ""quartz_cron_expression"": ""20 30 * * * ?"",
                    ""timezone_id"": ""Europe/London"",
                    ""pause_status"": ""PAUSED""
                },
                ""max_concurrent_runs"": 10,
                ""git_source"": null,
                ""format"": ""MULTI_TASK"",
                ""access_control_list"": [{
                        ""user_name"": ""jsmith@example.com"",
                        ""permission_level"": ""CAN_MANAGE""
                    },
                    {
                        ""group_name"": ""readonly-group@example.com"",
                        ""permission_level"": ""CAN_VIEW""
                    }
                ]
            }
            ";

    private static IEnumerable<AccessControlRequest> CreateDefaultAccessControlRequest()
    {
        return new AccessControlRequest[]
        {
            new AccessControlRequestForUser
                {PermissionLevel = JobPermissionLevel.CAN_MANAGE, UserName = "jsmith@example.com"},
            new AccessControlRequestForGroup
                {PermissionLevel = JobPermissionLevel.CAN_VIEW, GroupName = "readonly-group@example.com"}
        };
    }

    private static JobSettings CreateDefaultJobSettings()
    {
        var cluster = new ClusterAttributes()
            .WithRuntimeVersion(RuntimeVersions.Runtime_7_3)
            .WithNodeType(NodeTypes.Standard_D3_v2)
            .WithAutoScale(2, 16);
        cluster.SparkConfiguration = new Dictionary<string, string> {{"spark.speculation", "true"}};
        cluster.AzureAttributes = new AzureAttributes
            {Availability = AzureAvailability.ON_DEMAND_AZURE, FirstOnDemand = 0, SpotBidMaxPrice = -1};

        var task1 = new JobTaskSettings
        {
            TaskKey = "Sessionize",
            Description = "Extracts session data from events",
            ExistingClusterId = "0923-164208-meows279",
            SparkJarTask = new SparkJarTask
            {
                MainClassName = "com.databricks.Sessionize",
                Parameters = new List<string> {"--data", "dbfs:/path/to/data.json"}
            },
            Libraries = new List<Library>
            {
                new JarLibrary
                {
                    Jar = "dbfs:/mnt/databricks/Sessionize.jar"
                }
            },
            TimeoutSeconds = 86400,
            MaxRetries = 3,
            MinRetryIntervalMilliSeconds = 2000,
            RetryOnTimeout = false
        };

        var task2 = new JobTaskSettings
        {
            TaskKey = "Orders_Ingest",
            Description = "Ingests order data",
            JobClusterKey = "auto_scaling_cluster",
            SparkJarTask = new SparkJarTask
            {
                MainClassName = "com.databricks.OrdersIngest",
                Parameters = new List<string> {"--data", "dbfs:/path/to/order-data.json"}
            },
            Libraries = new List<Library>
            {
                new JarLibrary
                {
                    Jar = "dbfs:/mnt/databricks/OrderIngest.jar"
                }
            },
            TimeoutSeconds = 86400,
            MaxRetries = 3,
            MinRetryIntervalMilliSeconds = 2000,
            RetryOnTimeout = false
        };

        var task3 = new JobTaskSettings
        {
            TaskKey = "Match",
            Description = "Matches orders with user sessions",
            DependsOn = new[] {task2, task1},
            NewCluster = cluster,
            NotebookTask = new NotebookTask
            {
                NotebookPath = "/Users/user.name@databricks.com/Match",
                BaseParameters = new Dictionary<string, string>
                {
                    {"name", "John Doe"}, {"age", "35"}
                }
            },
            TimeoutSeconds = 86400,
            MaxRetries = 3,
            MinRetryIntervalMilliSeconds = 2000,
            RetryOnTimeout = false
        };

        var job = new JobSettings
        {
            Name = "A multitask job",
            Tags = new Dictionary<string, string> {{"cost-center", "engineering"}, {"team", "jobs"}},
            JobClusters = new List<JobCluster>
            {
                new()
                {
                    JobClusterKey = "auto_scaling_cluster",
                    NewCluster = cluster
                }
            },
            EmailNotifications = new JobEmailNotifications
            {
                NoAlertForSkippedRuns = false,
                OnStart = new[] {"user.name@databricks.com"},
                OnFailure = new[] {"user.name@databricks.com"},
                OnSuccess = new[] {"user.name@databricks.com"}
            },
            TimeoutSeconds = 86400,
            Schedule = new CronSchedule
            {
                QuartzCronExpression = "20 30 * * * ?",
                PauseStatus = PauseStatus.PAUSED,
                TimezoneId = "Europe/London"
            },
            MaxConcurrentRuns = 10,
            Format = JobFormat.MULTI_TASK,
            Tasks = new[]
            {
                task1, task2, task3
            }
        };

        return job;
    }
    
    #endregion

    [TestMethod]
    public async Task TestCreate()
    {
        var apiUri = new Uri(JobsApiUri, "create");
        var expectedResponse = new { job_id = 11223344 };

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Post, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse, Options),
                "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);

        var job = CreateDefaultJobSettings();
        var acr = CreateDefaultAccessControlRequest();
        var jobId = await client.Create(job, acr);
        Assert.AreEqual(expectedResponse.job_id, jobId);

        handler.VerifyRequest(
            HttpMethod.Post,
            apiUri,
            GetMatcher(MultiTaskJobJson),
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestReset()
    {
        var apiUri = new Uri(JobsApiUri, "reset");

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Post, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);
        
        var job = CreateDefaultJobSettings();

        await client.Reset(11223344, job);

        var jobReset = JsonNode.Parse(MultiTaskJobJson)!.AsObject();
        jobReset.Remove("access_control_list");
        var request = new JsonObject {new("job_id", JsonValue.Create(11223344)), new("new_settings", jobReset)};
            
        handler.VerifyRequest(
            HttpMethod.Post,
            apiUri,
            GetMatcher(request.ToJsonString(Options)),
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestUpdate()
    {
        var apiUri = new Uri(JobsApiUri, "update");

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Post, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);

        var job = CreateDefaultJobSettings();

        await client.Update(11223344, job, new []{ "libraries", "schedule" });

        var jobReset = JsonNode.Parse(MultiTaskJobJson)!.AsObject();
        jobReset.Remove("access_control_list");
        var request = new JsonObject
        {
            new("job_id", JsonValue.Create(11223344)),
            new("new_settings", jobReset),
            new("fields_to_remove", new JsonArray("libraries", "schedule"))
        };

        handler.VerifyRequest(
            HttpMethod.Post,
            apiUri,
            GetMatcher(request.ToJsonString(Options)),
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestDelete()
    {
        const string expectedRequest = @"
            {
              ""job_id"": 11223344
            }
        ";

        var apiUri = new Uri(JobsApiUri, "delete");
        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Post, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);
        await client.Delete(11223344);

        handler.VerifyRequest(
            HttpMethod.Post,
            apiUri,
            GetMatcher(expectedRequest),
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestGet()
    {
        var apiUri = new Uri(JobsApiUri, "get");

        var expectedResponse = new Job
        {
            CreatedTime = DateTimeOffset.FromUnixTimeMilliseconds(0),
            CreatorUserName = "user.name@databricks.com",
            JobId = 11223344,
            RunAsUserName = "user.name@databricks.com",
            Settings = CreateDefaultJobSettings()
        };

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Get, new Uri(apiUri, "?job_id=11223344"))
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse, Options), "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);

        var job = await client.Get(11223344);

        AssertJsonDeepEquals(
            JsonSerializer.Serialize(expectedResponse, Options),
            JsonSerializer.Serialize(job, Options)
        );

        handler.VerifyRequest(
            HttpMethod.Get,
            new Uri(apiUri, "?job_id=11223344"),
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestList()
    {
        var apiUri = new Uri(JobsApiUri, "list");

        var expectedResponse = new JobList
            {
                Jobs = new[]
                {
                    new Job
                    {
                        CreatedTime = DateTimeOffset.FromUnixTimeMilliseconds(0),
                        CreatorUserName = "user.name@databricks.com",
                        JobId = 11223344,
                        RunAsUserName = "user.name@databricks.com",
                        Settings = CreateDefaultJobSettings()
                    }
                },
                HasMore = false
            };

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Get, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse, Options), "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);

        var job = await client.List();

        AssertJsonDeepEquals(
            JsonSerializer.Serialize(expectedResponse, Options),
            JsonSerializer.Serialize(job, Options)
        );

        handler.VerifyRequest(
            HttpMethod.Get,
            apiUri,
            Times.Once()
        );
    }

    [TestMethod]
    public async Task TestRunNow()
    {
        var apiUri = new Uri(JobsApiUri, "run-now");

        const string expectedRequest = @"
            {
              ""job_id"": 11223344,
              ""idempotency_token"": ""8f018174-4792-40d5-bcbc-3e6a527352c8"",
              ""jar_params"": [
                ""john"",
                ""doe"",
                ""35""
              ],
              ""notebook_params"": {
                ""name"": ""john doe"",
                ""age"": ""35""
              },
              ""python_params"": [
                ""john doe"",
                ""35""
              ],
              ""spark_submit_params"": [
                ""--class"",
                ""org.apache.spark.examples.SparkPi""
              ],
              ""python_named_params"": {
                ""name"": ""task"",
                ""data"": ""dbfs:/path/to/data.json""
              }
            }
        ";

        const string expectedResponse = @"
            {
              ""run_id"": 455644833,
              ""number_in_job"": 455644833
            }
        ";

        var runParams = new RunParameters
        {
            JarParams = new List<string> {"john", "doe", "35"},
            NotebookParams = new Dictionary<string, string> {{"name", "john doe"}, {"age", "35"}},
            PythonParams = new List<string> {"john doe", "35"},
            SparkSubmitParams = new List<string> {"--class", "org.apache.spark.examples.SparkPi"},
            PythonNamedParams = new Dictionary<string, string> {{"name", "task"}, {"data", "dbfs:/path/to/data.json"}}
        };

        var handler = CreateMockHandler();
        handler
            .SetupRequest(HttpMethod.Post, apiUri)
            .ReturnsResponse(HttpStatusCode.OK, expectedResponse, "application/json")
            .Verifiable();

        var hc = handler.CreateClient();
        hc.BaseAddress = BaseApiUri;

        using var client = new JobsApiClient(hc);
        var runId = await client.RunNow(11223344, runParams, "8f018174-4792-40d5-bcbc-3e6a527352c8");

        Assert.AreEqual(455644833, runId.RunId);

        handler.VerifyRequest(
            HttpMethod.Post,
            apiUri,
            GetMatcher(expectedRequest),
            Times.Once()
        );
    }
}