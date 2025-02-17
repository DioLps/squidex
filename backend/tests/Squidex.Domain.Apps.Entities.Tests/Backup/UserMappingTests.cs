﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.TestHelpers;
using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Infrastructure;
using Squidex.Shared.Users;

namespace Squidex.Domain.Apps.Entities.Backup;

public class UserMappingTests : GivenContext
{
    private readonly RefToken initiator = RefToken.User("me");
    private readonly UserMapping sut;

    public UserMappingTests()
    {
        sut = new UserMapping(initiator);
    }

    [Fact]
    public async Task Should_backup_users_but_no_clients()
    {
        sut.Backup("1");
        sut.Backup(RefToken.User("2"));

        sut.Backup(RefToken.Client("client"));

        var user1 = UserMocks.User("1", "1@email.com");
        var user2 = UserMocks.User("2", "1@email.com");

        var users = new Dictionary<string, IUser>
        {
            [user1.Id] = user1,
            [user2.Id] = user2,
        };

        var userResolver = A.Fake<IUserResolver>();

        A.CallTo(() => userResolver.QueryManyAsync(A<string[]>.That.Is(user1.Id, user2.Id), CancellationToken))
            .Returns(users);

        var writer = A.Fake<IBackupWriter>();

        Dictionary<string, string>? storedUsers = null;

        A.CallTo(() => writer.WriteJsonAsync(A<string>._, A<object>._, CancellationToken))
            .Invokes(x => storedUsers = x.GetArgument<Dictionary<string, string>>(1));

        await sut.StoreAsync(writer, userResolver, CancellationToken);

        Assert.Equal(new Dictionary<string, string>
        {
            [user1.Id] = user1.Email,
            [user2.Id] = user2.Email,
        }, storedUsers);
    }

    [Fact]
    public async Task Should_restore_users()
    {
        var user1 = UserMocks.User("1", "1@email.com");
        var user2 = UserMocks.User("2", "2@email.com");

        var reader = SetupReader(user1, user2);

        var userResolver = A.Fake<IUserResolver>();

        A.CallTo(() => userResolver.CreateUserIfNotExistsAsync(user1.Email, false, CancellationToken))
            .Returns((user1, false));

        A.CallTo(() => userResolver.CreateUserIfNotExistsAsync(user2.Email, false, CancellationToken))
            .Returns((user2, true));

        await sut.RestoreAsync(reader, userResolver, CancellationToken);

        Assert.True(sut.TryMap("1_old", out var mapped1));
        Assert.True(sut.TryMap(RefToken.User("2_old"), out var mapped2));

        Assert.Equal(RefToken.User("1"), mapped1);
        Assert.Equal(RefToken.User("2"), mapped2);
    }

    [Fact]
    public void Should_return_initiator_if_user_not_found()
    {
        var user = RefToken.User("user1");

        Assert.False(sut.TryMap(user, out var mapped));
        Assert.Same(initiator, mapped);
    }

    [Fact]
    public void Should_create_same_token_if_mapping_client()
    {
        var client = RefToken.Client("client1");

        Assert.True(sut.TryMap(client, out var mapped));
        Assert.Same(client, mapped);
    }

    private IBackupReader SetupReader(params IUser[] users)
    {
        var storedUsers = users.ToDictionary(x => $"{x.Id}_old", x => x.Email);

        var reader = A.Fake<IBackupReader>();

        A.CallTo(() => reader.ReadJsonAsync<Dictionary<string, string>>(A<string>._, CancellationToken))
            .Returns(storedUsers);

        return reader;
    }
}
