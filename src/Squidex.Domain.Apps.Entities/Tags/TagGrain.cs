﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Orleans;
using Squidex.Infrastructure.States;

namespace Squidex.Domain.Apps.Entities.Tags
{
    public sealed class TagGrain : GrainOfString, ITagGrain
    {
        private readonly IStore<string> store;
        private IPersistence<State> persistence;
        private State state = new State();

        [CollectionName("Index_Tags")]
        public sealed class State
        {
            public Dictionary<string, TagInfo> Tags { get; set; } = new Dictionary<string, TagInfo>();
        }

        public sealed class TagInfo
        {
            public string Name { get; set; }

            public int Count { get; set; } = 1;
        }

        public TagGrain(IStore<string> store)
        {
            Guard.NotNull(store, nameof(store));

            this.store = store;
        }

        public override Task OnActivateAsync(string key)
        {
            persistence = store.WithSnapshots<TagGrain, State, string>(key, s =>
            {
                state = s;
            });

            return persistence.ReadAsync();
        }

        public async Task<string[]> NormalizeTagsAsync(string[] names, string[] ids)
        {
            var result = new List<string>();

            if (names != null)
            {
                foreach (var tag in names)
                {
                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        var tagName = tag.ToLowerInvariant();
                        var tagId = string.Empty;

                        var found = state.Tags.FirstOrDefault(x => string.Equals(x.Value.Name, tagName, StringComparison.OrdinalIgnoreCase));

                        if (found.Value != null)
                        {
                            tagId = found.Key;
                        }
                        else
                        {
                            tagId = Guid.NewGuid().ToString();

                            state.Tags.Add(tagId, new TagInfo { Name = tagName });
                        }

                        result.Add(tagId);
                    }
                }
            }

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    if (!result.Contains(id))
                    {
                        if (state.Tags.TryGetValue(id, out var tagInfo))
                        {
                            tagInfo.Count--;

                            if (tagInfo.Count <= 0)
                            {
                                state.Tags.Remove(id);
                            }
                        }
                    }
                }
            }

            await persistence.WriteSnapshotAsync(state);

            return result.ToArray();
        }

        public Task<string[]> GetTagIdsAsync(string[] names)
        {
            var result = new List<string>();

            foreach (var name in names)
            {
                result.Add(state.Tags.FirstOrDefault(x => x.Value.Name == name).Key);
            }

            return Task.FromResult(result.ToArray());
        }

        public Task<Dictionary<string, string>> DenormalizeTagsAsync(string[] ids)
        {
            var result = new Dictionary<string, string>();

            foreach (var id in ids)
            {
                if (state.Tags.TryGetValue(id, out var tagInfo))
                {
                    result[id] = tagInfo.Name;
                }
            }

            return Task.FromResult(result);
        }

        public Task<Dictionary<string, int>> GetTagsAsync()
        {
            return Task.FromResult(state.Tags.Values.ToDictionary(x => x.Name, x => x.Count));
        }
    }
}
