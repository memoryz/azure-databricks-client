using Microsoft.Azure.Databricks.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Databricks.Client
{
    public class JobsApiClient : ApiClient, IJobsApi
    {
        protected override string ApiVersion => "2.1";

        public JobsApiClient(HttpClient httpClient) : base(httpClient)
        {
        }

        public async Task<long> Create(JobSettings jobSettings,
            IEnumerable<AccessControlRequest> accessControlList = default,
            CancellationToken cancellationToken = default)
        {
            var request = JsonSerializer.SerializeToNode(jobSettings, Options)!.AsObject();

            accessControlList.ForEach(acr =>
            {
                request.Add(new KeyValuePair<string, JsonNode>(
                    "access_control_list",
                    JsonSerializer.SerializeToNode(acr, Options)
                ));
            });

            var jobIdentifier =
                await HttpPost<JsonObject, JsonObject>(this.HttpClient, $"{ApiVersion}/jobs/create", request,
                        cancellationToken)
                    .ConfigureAwait(false);
            return jobIdentifier["job_id"]!.GetValue<long>();
        }

        public async Task<JobList> List(int limit = 20, int offset = 0, bool expandTasks = false,
            CancellationToken cancellationToken = default)
        {
            var requestUri = $"{ApiVersion}/jobs/list";
            var response = await HttpGet<JsonObject>(this.HttpClient, requestUri, cancellationToken)
                .ConfigureAwait(false);

            response.TryGetPropertyValue("jobs", out var jobsNode);
            var jobs = jobsNode
                .Map(node => node.Deserialize<IEnumerable<Job>>(Options))
                .GetOrElse(Enumerable.Empty<Job>);

            response.TryGetPropertyValue("has_more", out var hasMoreNode);
            var hasMore = hasMoreNode.Exists(node => node.GetValue<bool>());
            
            return new JobList {Jobs = jobs, HasMore = hasMore};
        }

        public async Task Delete(long jobId, CancellationToken cancellationToken = default)
        {
            await HttpPost(this.HttpClient, $"{ApiVersion}/jobs/delete", new { job_id = jobId }, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Job> Get(long jobId, CancellationToken cancellationToken = default)
        {
            var requestUri = $"{ApiVersion}/jobs/get?job_id={jobId}";
            return await HttpGet<Job>(this.HttpClient, requestUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task Reset(long jobId, JobSettings newSettings, CancellationToken cancellationToken = default)
        {
            await HttpPost(this.HttpClient, $"{ApiVersion}/jobs/reset", new { job_id = jobId, new_settings = newSettings }, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task Update(long jobId, JobSettings newSettings, string[] fieldsToRemove = default,
            CancellationToken cancellationToken = default)
        {
            await HttpPost(this.HttpClient, $"{ApiVersion}/jobs/update",
                    new { job_id = jobId, new_settings = newSettings, fields_to_remove = fieldsToRemove },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        //public async Task<RunIdentifier> RunNow(long jobId, RunParameters runParams, CancellationToken cancellationToken = default)
        //{
        //    var settings = new RunNowSettings
        //    {
        //        JobId = jobId
        //    };

        //    if (runParams != null)
        //    {
        //        settings.SparkSubmitParams = runParams.SparkSubmitParams;
        //        settings.PythonParams = runParams.PythonParams;
        //        settings.NotebookParams = runParams.NotebookParams;
        //        settings.JarParams = runParams.JarParams;
        //    }

        //    return await HttpPost<RunNowSettings, RunIdentifier>(this.HttpClient, $"{ApiVersion}/jobs/run-now", settings, cancellationToken)
        //        .ConfigureAwait(false);
        //}

        //public async Task<long> RunSubmit(RunOnceSettings settings, CancellationToken cancellationToken = default)
        //{
        //    var result = await HttpPost<RunOnceSettings, RunIdentifier>(this.HttpClient, $"{ApiVersion}/jobs/runs/submit", settings, cancellationToken)
        //        .ConfigureAwait(false);
        //    return result.RunId;
        //}

        //public async Task<RunList> RunsList(long? jobId = null, int offset = 0, int limit = 20, bool activeOnly = false,
        //    bool completedOnly = false, /*RunType? runType = null, */ CancellationToken cancellationToken = default)
        //{
        //    if (activeOnly && completedOnly)
        //    {
        //        throw new ArgumentException(
        //            $"{nameof(activeOnly)} and {nameof(completedOnly)} cannot both be true.");
        //    }

        //    var url = $"{ApiVersion}/jobs/runs/list?limit={limit}&offset={offset}";
        //    if (jobId.HasValue)
        //    {
        //        url += $"&job_id={jobId.Value}";
        //    }

        //    if (activeOnly)
        //    {
        //        url += "&active_only=true";
        //    }

        //    if (completedOnly)
        //    {
        //        url += "&completed_only=true";
        //    }

        //    // if (runType.HasValue)
        //    // {
        //    //     url += $"&run_type={runType.Value}";
        //    // }

        //    return await HttpGet<RunList>(this.HttpClient, url, cancellationToken).ConfigureAwait(false);
        //}

        //public async Task<Run> RunsGet(long runId, CancellationToken cancellationToken = default)
        //{
        //    var url = $"{ApiVersion}/jobs/runs/get?run_id={runId}";
        //    return await HttpGet<Run>(this.HttpClient, url, cancellationToken).ConfigureAwait(false);
        //}

        //public async Task RunsCancel(long runId, CancellationToken cancellationToken = default)
        //{
        //    var request = new { run_id = runId };
        //    await HttpPost(this.HttpClient, $"{ApiVersion}/jobs/runs/cancel", request, cancellationToken).ConfigureAwait(false);
        //}

        //public async Task RunsDelete(long runId, CancellationToken cancellationToken = default)
        //{
        //    var request = new { run_id = runId };
        //    await HttpPost(this.HttpClient, $"{ApiVersion}/jobs/runs/delete", request, cancellationToken).ConfigureAwait(false);
        //}

        //public async Task<IEnumerable<ViewItem>> RunsExport(long runId,
        //    ViewsToExport viewsToExport = ViewsToExport.CODE, CancellationToken cancellationToken = default)
        //{
        //    var url = $"{ApiVersion}/jobs/runs/export?run_id={runId}&views_to_export={viewsToExport}";
        //    var viewItemList = await HttpGet<JsonObject>(this.HttpClient, url, cancellationToken).ConfigureAwait(false);

        //    if (viewItemList.TryGetPropertyValue("views", out var views))
        //    {
        //        return views.Deserialize<IEnumerable<ViewItem>>(Options);
        //    }
        //    else
        //    {
        //        return Enumerable.Empty<ViewItem>();
        //    }
        //}

        //public async Task<(string, string, Run)> RunsGetOutput(long runId, CancellationToken cancellationToken = default)
        //{
        //    var url = $"{ApiVersion}/jobs/runs/get-output?run_id={runId}";
        //    var response = await HttpGet<JsonObject>(this.HttpClient, url, cancellationToken).ConfigureAwait(false);
        //    Run run = response["metadata"].Deserialize<Run>(Options);

        //    string errStr = null;
        //    if (response.TryGetPropertyValue("error", out var error))
        //    {
        //        errStr = error.GetValue<string>();
        //    }

        //    string notebookOutput = null;
        //    if (response.TryGetPropertyValue("notebook_output", out var notebook_output) &&
        //        notebook_output.AsObject().TryGetPropertyValue("result", out var result))
        //    {
        //        notebookOutput = result.GetValue<string>();
        //    }

        //    return (notebookOutput, errStr, run);
        //}
    }
}
