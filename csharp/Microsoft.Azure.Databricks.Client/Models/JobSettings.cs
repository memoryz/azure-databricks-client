﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Databricks.Client.Models
{
    public record HasTaskKey
    {
        /// <summary>
        /// A unique name for the task. This field is used to refer to this task from other tasks. This field is required and must be unique within its parent job.
        /// On Update or Reset, this field is used to reference the tasks to be updated or reset. The maximum length is 100 characters.
        /// </summary>
        [JsonPropertyName("task_key")]
        public string TaskKey { get; set; }
    }

    public record TaskSettings : HasTaskKey
    {
        [JsonPropertyName("depends_on")]
        public IEnumerable<HasTaskKey> DependsOn { get; set; }

        [JsonPropertyName("existing_cluster_id")]
        public string ExistingClusterId { get; set; }

        [JsonPropertyName("new_cluster")]
        public ClusterAttributes NewCluster { get; set; }

        /// <summary>
        /// If set, indicates that this task must run a notebook. This field may not be specified in conjunction with spark_jar_task.
        /// </summary>
        [JsonPropertyName("notebook_task")]
        public NotebookTask NotebookTask { get; set; }

        /// <summary>
        /// If set, indicates that this task must run a JAR.
        /// </summary>
        [JsonPropertyName("spark_jar_task")]
        public SparkJarTask SparkJarTask { get; set; }

        /// <summary>
        /// If set, indicates that this task must run a Python file.
        /// </summary>
        [JsonPropertyName("spark_python_task")]
        public SparkPythonTask SparkPythonTask { get; set; }

        /// <summary>
        /// If set, indicates that this task must be launched by the spark submit script.
        /// </summary>
        [JsonPropertyName("spark_submit_task")]
        public SparkSubmitTask SparkSubmitTask { get; set; }

        /// <summary>
        /// If set, indicates that this task must execute a Pipeline.
        /// </summary>
        [JsonPropertyName("pipeline_task")]
        public PipelineTask PipelineTask { get; set; }

        /// <summary>
        /// If set, indicates that this job must execute a PythonWheel.
        /// </summary>
        [JsonPropertyName("python_wheel_task")]
        public PythonWheelTask PythonWheelTask { get; set; }

        /// <summary>
        /// An optional list of libraries to be installed on the cluster that executes the task. The default value is an empty list.
        /// </summary>
        [JsonPropertyName("libraries")]
        public List<Library> Libraries { get; set; }

        /// <summary>
        /// An optional timeout applied to each run of this job task. The default behavior is to have no timeout.
        /// </summary>
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; }
    }

    public record JobTaskSettings : TaskSettings
    {
        [JsonPropertyName("job_cluster_key")]
        public string JobClusterKey { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// An optional set of email addresses that will be notified when runs of this job begin or complete as well as when this job is deleted. The default behavior is to not send any emails.
        /// </summary>
        [JsonPropertyName("email_notifications")]
        public JobEmailNotifications EmailNotifications { get; set; }

        /// <summary>
        /// An optional maximum number of times to retry an unsuccessful run. A run is considered to be unsuccessful if it completes with a FAILED result_state or INTERNAL_ERROR life_cycle_state. The value -1 means to retry indefinitely and the value 0 means to never retry. The default behavior is to never retry.
        /// </summary>
        [JsonPropertyName("max_retries")]
        public int MaxRetries { get; set; }

        /// <summary>
        /// An optional minimal interval in milliseconds between attempts. The default behavior is that unsuccessful runs are immediately retried.
        /// </summary>
        [JsonPropertyName("min_retry_interval_millis")]
        public int MinRetryIntervalMilliSeconds { get; set; }

        /// <summary>
        /// An optional policy to specify whether to retry a job when it times out. The default behavior is to not retry on timeout.
        /// </summary>
        [JsonPropertyName("retry_on_timeout")]
        public bool RetryOnTimeout { get; set; }
    }

    public record JobCluster
    {
        /// <summary>
        /// A unique name for the job cluster. This field is required and must be unique within the job.
        /// `JobTaskSettings` may refer to this field to determine which cluster to launch for the task execution.
        /// </summary>
        [JsonPropertyName("job_cluster_key")]
        public string JobClusterKey { get; set; }

        [JsonPropertyName("new_cluster")]
        public ClusterAttributes NewCluster { get; set; }
    }

    /// <summary>
    /// Settings for a job. These settings can be updated using the resetJob method.
    /// </summary>
    public record JobSettings: IJsonOnDeserialized
    {
        //public static JobSettings GetNewSparkJarJobSettings(string jobName, string mainClass,
        //    IEnumerable<string> parameters, IEnumerable<string> jarLibs)
        //{
        //    var jobSettings = new JobSettings
        //    {
        //        Name = jobName,
        //        SparkJarTask = new SparkJarTask
        //        {
        //            MainClassName = mainClass,
        //            Parameters = parameters?.ToList()
        //        },
        //        Libraries = jarLibs?.Select(jarLib => new JarLibrary { Jar = jarLib }).Cast<Library>().ToList(),
        //        SparkPythonTask = null,
        //        SparkSubmitTask = null,
        //        NotebookTask = null
        //    };

        //    return jobSettings;
        //}

        //public static JobSettings GetNewNotebookJobSettings(string jobName, string notebookPath,
        //    Dictionary<string, string> parameters)
        //{
        //    var jobSettings = new JobSettings
        //    {
        //        Name = jobName,
        //        NotebookTask = new NotebookTask
        //        {
        //            NotebookPath = notebookPath,
        //            BaseParameters = parameters
        //        },
        //        SparkJarTask = null,
        //        SparkPythonTask = null,
        //        SparkSubmitTask = null,
        //        Libraries = null
        //    };

        //    return jobSettings;
        //}

        /// <summary>
        /// Adds a cron schedule to a job
        /// </summary>
        /// <param name="cronSchedule"></param>
        /// <returns></returns>
        public JobSettings WithSchedule(CronSchedule cronSchedule)
        {
            Schedule = cronSchedule;
            return this;
        }

        public void OnDeserialized()
        {
            var taskMap = this.Tasks.ToDictionary(
                task => task.TaskKey,
                task => task
            );

            foreach (var task in Tasks)
            {
                task.DependsOn = from dep in task.DependsOn ?? Enumerable.Empty<HasTaskKey>()
                                 select taskMap[dep.TaskKey];
            }
        }

        /// <summary>
        /// An optional name for the job. The default value is Untitled.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// A map of tags associated with the job. These are forwarded to the cluster as cluster tags for jobs clusters, and are subject to the same limitations as cluster tags. A maximum of 25 tags can be added to the job.
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// A list of task specifications to be executed by this job.
        /// </summary>
        [JsonPropertyName("tasks")]
        public IEnumerable<JobTaskSettings> Tasks { get; set; }

        /// <summary>
        /// A list of job cluster specifications that can be shared and reused by tasks of this job. Libraries cannot be declared in a shared job cluster. You must declare dependent libraries in task settings.
        /// </summary>
        [JsonPropertyName("job_clusters")]
        public IEnumerable<JobCluster> JobClusters { get; set; }

        /// <summary>
        /// An optional set of email addresses that will be notified when runs of this job begin or complete as well as when this job is deleted. The default behavior is to not send any emails.
        /// </summary>
        [JsonPropertyName("email_notifications")]
        public JobEmailNotifications EmailNotifications { get; set; }

        /// <summary>
        /// An optional timeout applied to each run of this job. The default behavior is to have no timeout.
        /// </summary>
        [JsonPropertyName("timeout_seconds")]
        public int TimeoutSeconds { get; set; }

        /// <summary>
        /// An optional periodic schedule for this job. The default behavior is that the job only runs when triggered by clicking "Run Now" in the Jobs UI or sending an API request to `runNow`.
        /// </summary>
        [JsonPropertyName("schedule")]
        public CronSchedule Schedule { get; set; }

        /// <summary>
        /// An optional maximum allowed number of concurrent runs of the job.
        /// Set this value if you want to be able to execute multiple runs of the same job concurrently. This is useful for example if you trigger your job on a frequent schedule and want to allow consecutive runs to overlap with each other, or if you want to trigger multiple runs which differ by their input parameters.
        /// This setting affects only new runs. For example, suppose the job’s concurrency is 4 and there are 4 concurrent active runs. Then setting the concurrency to 3 won’t kill any of the active runs. However, from then on, new runs will be skipped unless there are fewer than 3 active runs.
        /// This value cannot exceed 1000. Setting this value to 0 will cause all new runs to be skipped. The default behavior is to allow only 1 concurrent run.
        /// </summary>
        [JsonPropertyName("max_concurrent_runs")]
        public int? MaxConcurrentRuns { get; set; }

        /// <summary>
        /// An optional specification for a remote repository containing the notebooks used by this job's notebook tasks.
        /// </summary>
        [JsonPropertyName("git_source")]
        public GitSource GitSource { get; set; }

        /// <summary>
        /// Used to tell what is the format of the job. This field is ignored in Create/Update/Reset calls. When using the Jobs API 2.1 this value is always set to `"MULTI_TASK"`.
        /// </summary>
        [JsonPropertyName("format")]
        public JobFormat Format { get; set; }

        /// <summary>
        /// List of permissions to set on the job.
        /// </summary>
        [JsonPropertyName("access_control_list")]
        public IEnumerable<AccessControlRequest> AccessControlList { get; set; }
    }

    public enum JobFormat
    {
        SINGLE_TASK,
        MULTI_TASK
    }
}
