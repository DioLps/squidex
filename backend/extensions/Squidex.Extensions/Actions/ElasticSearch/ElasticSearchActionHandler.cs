﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text.Json.Serialization;
using Elasticsearch.Net;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Scripting;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Json;

#pragma warning disable IDE0059 // Value assigned to symbol is never used
#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Extensions.Actions.ElasticSearch;

public sealed class ElasticSearchActionHandler(RuleEventFormatter formatter, IScriptEngine scriptEngine, IJsonSerializer serializer) : RuleActionHandler<ElasticSearchAction, ElasticSearchJob>(formatter)
{
    private readonly ClientPool<(Uri Host, string? Username, string? Password), ElasticLowLevelClient> clients = new ClientPool<(Uri Host, string? Username, string? Password), ElasticLowLevelClient>(key =>
        {
            var config = new ConnectionConfiguration(key.Host);

            if (!string.IsNullOrEmpty(key.Username) && !string.IsNullOrWhiteSpace(key.Password))
            {
                config = config.BasicAuthentication(key.Username, key.Password);
            }

            return new ElasticLowLevelClient(config);
        });

    protected override async Task<(string Description, ElasticSearchJob Data)> CreateJobAsync(EnrichedEvent @event, ElasticSearchAction action)
    {
        var delete = @event.ShouldDelete(scriptEngine, action.Delete);

        string contentId;

        if (@event is IEnrichedEntityEvent enrichedEntityEvent)
        {
            contentId = enrichedEntityEvent.Id.ToString();
        }
        else
        {
            contentId = DomainId.NewGuid().ToString();
        }

        var ruleText = string.Empty;
        var ruleJob = new ElasticSearchJob
        {
            IndexName = (await FormatAsync(action.IndexName, @event))!,
            ServerHost = action.Host.ToString(),
            ServerUser = action.Username,
            ServerPassword = action.Password,
            ContentId = contentId,
        };

        if (delete)
        {
            ruleText = $"Delete entry index: {ruleJob.IndexName}";
        }
        else
        {
            ruleText = $"Upsert to index: {ruleJob.IndexName}";

            ElasticSearchContent content;
            try
            {
                string? jsonString;

                if (!string.IsNullOrEmpty(action.Document))
                {
                    jsonString = await FormatAsync(action.Document, @event);
                    jsonString = jsonString?.Trim();
                }
                else
                {
                    jsonString = ToJson(@event);
                }

                content = serializer.Deserialize<ElasticSearchContent>(jsonString!);
            }
            catch (Exception ex)
            {
                content = new ElasticSearchContent
                {
                    More = new Dictionary<string, object>
                    {
                        ["error"] = $"Invalid JSON: {ex.Message}",
                    },
                };
            }

            content.ContentId = contentId;

            ruleJob.Content = serializer.Serialize(content, true);
        }

        return (ruleText, ruleJob);
    }

    protected override async Task<Result> ExecuteJobAsync(ElasticSearchJob job,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(job.ServerHost))
        {
            return Result.Ignored();
        }

        var client = await clients.GetClientAsync((new Uri(job.ServerHost, UriKind.Absolute), job.ServerUser, job.ServerPassword));

        try
        {
            if (job.Content != null)
            {
                var response = await client.IndexAsync<StringResponse>(job.IndexName, job.ContentId, job.Content, ctx: ct);

                return Result.SuccessOrFailed(response.OriginalException, response.Body);
            }
            else
            {
                var response = await client.DeleteAsync<StringResponse>(job.IndexName, job.ContentId, ctx: ct);

                return Result.SuccessOrFailed(response.OriginalException, response.Body);
            }
        }
        catch (ElasticsearchClientException ex)
        {
            return Result.Failed(ex);
        }
    }
}

public sealed class ElasticSearchContent
{
    public string ContentId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object> More { get; set; } = [];
}

public sealed class ElasticSearchJob
{
    public string ServerHost { get; set; }

    public string? ServerUser { get; set; }

    public string? ServerPassword { get; set; }

    public string ContentId { get; set; }

    public string Content { get; set; }

    public string IndexName { get; set; }
}
