﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.Extensions.Options;
using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Entities.Apps.DomainObject;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Domain.Apps.Events.Apps;
using Squidex.Events;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.EventSourcing;

namespace Squidex.Domain.Apps.Entities.Apps;

public class AppPermanentDeleterTests : GivenContext
{
    private readonly IDomainObjectFactory domainObjectFactory = A.Fake<IDomainObjectFactory>();
    private readonly IDeleter deleter1 = A.Fake<IDeleter>();
    private readonly IDeleter deleter2 = A.Fake<IDeleter>();
    private readonly AppsOptions options = new AppsOptions();
    private readonly AppPermanentDeleter sut;

    public AppPermanentDeleterTests()
    {
        sut = new AppPermanentDeleter([deleter1, deleter2], Options.Create(options), domainObjectFactory, TestUtils.TypeRegistry);
    }

    [Fact]
    public void Should_return_assets_filter_for_events_filter()
    {
        Assert.Equal(StreamFilter.Prefix("app-"), sut.EventsFilter);
    }

    [Fact]
    public async Task Should_do_nothing_on_clear()
    {
        await ((IEventConsumer)sut).ClearAsync();
    }

    [Fact]
    public void Should_return_type_name_for_name()
    {
        Assert.Equal(nameof(AppPermanentDeleter), ((IEventConsumer)sut).Name);
    }

    [Fact]
    public async Task Should_handle_delete_event()
    {
        var eventType = TestUtils.TypeRegistry.GetName<IEvent, AppDeleted>();

        var storedEvent =
            new StoredEvent("stream", "1", 1,
                new EventData(eventType, [], "payload"));

        Assert.True(await sut.HandlesAsync(storedEvent));
    }

    [Fact]
    public async Task Should_handle_contributor_event()
    {
        var eventType = TestUtils.TypeRegistry.GetName<IEvent, AppContributorRemoved>();

        var storedEvent =
            new StoredEvent("stream", "1", 1,
                new EventData(eventType, [], "payload"));

        Assert.True(await sut.HandlesAsync(storedEvent));
    }

    [Fact]
    public async Task Should_not_handle_creation_event()
    {
        var eventType = TestUtils.TypeRegistry.GetName<IEvent, AppCreated>();

        var storedEvent =
            new StoredEvent("stream", "1", 1,
                new EventData(eventType, [], "payload"));

        Assert.False(await sut.HandlesAsync(storedEvent));
    }

    [Fact]
    public async Task Should_call_deleters_when_contributor_removed()
    {
        await sut.On(Envelope.Create(new AppContributorRemoved
        {
            AppId = AppId,
            ContributorId = "user1",
        }));

        A.CallTo(() => deleter1.DeleteContributorAsync(AppId.Id, "user1", default))
            .MustHaveHappened();

        A.CallTo(() => deleter2.DeleteContributorAsync(AppId.Id, "user1", default))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_call_deleters_when_app_deleted_and_enabled_globally()
    {
        options.DeletePermanent = true;
        SetupDomainObject();

        await sut.On(Envelope.Create(new AppDeleted
        {
            AppId = AppId,
            Permanent = false,
        }));

        A.CallTo(() => deleter1.DeleteAppAsync(App, default))
            .MustHaveHappened();

        A.CallTo(() => deleter2.DeleteAppAsync(App, default))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_call_deleters_when_app_deleted_and_enabled_per_event()
    {
        options.DeletePermanent = false;
        SetupDomainObject();

        await sut.On(Envelope.Create(new AppDeleted
        {
            AppId = AppId,
            Permanent = true,
        }));

        A.CallTo(() => deleter1.DeleteAppAsync(App, default))
            .MustHaveHappened();

        A.CallTo(() => deleter2.DeleteAppAsync(App, default))
            .MustHaveHappened();
    }

    [Fact]
    public async Task Should_not_call_deleters_when_app_not_deleted_permanently_and_not_enabled_globally()
    {
        options.DeletePermanent = false;

        await sut.On(Envelope.Create(new AppDeleted
        {
            AppId = AppId,
            Permanent = false,
        }));

        A.CallTo(() => deleter1.DeleteAppAsync(App, default))
            .MustNotHaveHappened();

        A.CallTo(() => deleter2.DeleteAppAsync(App, default))
            .MustNotHaveHappened();
    }

    private void SetupDomainObject()
    {
        var domainObject = A.Fake<AppDomainObject>();

        A.CallTo(() => domainObject.Snapshot)
            .Returns(App);

        A.CallTo(() => domainObjectFactory.Create<AppDomainObject>(App.Id))
            .Returns(domainObject);
    }
}
