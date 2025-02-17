﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Text;
using Squidex.Assets;
using Squidex.Domain.Apps.Events.Assets;
using Squidex.Events;
using Squidex.Infrastructure;
using Squidex.Infrastructure.EventSourcing;

namespace Squidex.Domain.Apps.Entities.Assets;

public sealed class RebuildFiles(
    IAssetFileStore assetFileStore,
    IEventFormatter eventFormatter,
    IEventStore eventStore)
{
    private static readonly MemoryStream DummyStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy"));

    public async Task RepairAsync(
        CancellationToken ct = default)
    {
        var streamFilter = StreamFilter.Prefix("asset-");

        await foreach (var storedEvent in eventStore.QueryAllAsync(streamFilter, ct: ct))
        {
            var @event = eventFormatter.ParseIfKnown(storedEvent);

            if (@event != null)
            {
                switch (@event.Payload)
                {
                    case AssetCreated assetCreated:
                        await TryRepairAsync(assetCreated.AppId, assetCreated.AssetId, assetCreated.FileVersion, ct);
                        break;
                    case AssetUpdated assetUpdated:
                        await TryRepairAsync(assetUpdated.AppId, assetUpdated.AssetId, assetUpdated.FileVersion, ct);
                        break;
                }
            }
        }
    }

    private async Task TryRepairAsync(NamedId<DomainId> appId, DomainId id, long fileVersion,
        CancellationToken ct)
    {
        try
        {
            await assetFileStore.GetFileSizeAsync(appId.Id, id, fileVersion, null, ct);
        }
        catch (AssetNotFoundException)
        {
            DummyStream.Position = 0;

            await assetFileStore.UploadAsync(appId.Id, id, fileVersion, null, DummyStream, ct: ct);
        }
    }
}
