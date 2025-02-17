﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson;
using MongoDB.Driver;

namespace Squidex.Infrastructure.UsageTracking;

public sealed class MongoUsageRepository(IMongoDatabase database) : MongoRepositoryBase<MongoUsage>(database), IUsageRepository
{
    protected override string CollectionName()
    {
        return "UsagesV2";
    }

    protected override Task SetupCollectionAsync(IMongoCollection<MongoUsage> collection,
        CancellationToken ct)
    {
        return collection.Indexes.CreateOneAsync(
            new CreateIndexModel<MongoUsage>(
                Index
                    .Ascending(x => x.Key)
                    .Ascending(x => x.Category)
                    .Ascending(x => x.Date)),
            cancellationToken: ct);
    }

    public Task DeleteAsync(string key,
        CancellationToken ct = default)
    {
        Guard.NotNull(key);

        return Collection.DeleteManyAsync(x => x.Key == key, ct);
    }

    public Task DeleteByKeyPatternAsync(string pattern,
        CancellationToken ct = default)
    {
        Guard.NotNull(pattern);

        return Collection.DeleteManyAsync(Filter.Regex(x => x.Key, new BsonRegularExpression(pattern)), ct);
    }

    public async Task TrackUsagesAsync(UsageUpdate[] updates,
        CancellationToken ct = default)
    {
        Guard.NotNull(updates);

        var writes = new List<WriteModel<MongoUsage>>(updates.Length);

        foreach (var update in updates)
        {
            if (update.Counters.Count > 0)
            {
                var (filter, updateStatement) = CreateOperation(update);

                writes.Add(new UpdateOneModel<MongoUsage>(filter, updateStatement) { IsUpsert = true });
            }
        }

        if (writes.Count > 0)
        {
            await Collection.BulkWriteAsync(writes, BulkUnordered, ct);
        }
    }

    private static (FilterDefinition<MongoUsage>, UpdateDefinition<MongoUsage>) CreateOperation(UsageUpdate usageUpdate)
    {
        var id = $"{usageUpdate.Key}_{usageUpdate.Date:yyyy-MM-dd}_{usageUpdate.Category}";

        var update = Update
            .SetOnInsert(x => x.Key, usageUpdate.Key)
            .SetOnInsert(x => x.Date, usageUpdate.Date.ToDateTime(default))
            .SetOnInsert(x => x.Category, usageUpdate.Category);

        foreach (var (key, value) in usageUpdate.Counters)
        {
            update = update.Inc($"Counters.{key}", value);
        }

        var filter = Filter.Eq(x => x.Id, id);

        return (filter, update);
    }

    public async Task<IReadOnlyList<StoredUsage>> QueryAsync(string key, DateOnly fromDate, DateOnly toDate,
        CancellationToken ct = default)
    {
        var dateTimeFrom = fromDate.ToDateTime(default);
        var dateTimeTo = toDate.ToDateTime(default);

        var entities = await Collection
            .Find(x =>
                x.Key == key &&
                x.Date >= dateTimeFrom &&
                x.Date <= dateTimeTo)
            .ToListAsync(ct);

        return entities.Select(x => new StoredUsage(x.Category, x.Date.ToDateOnly(), x.Counters)).ToList();
    }
}
