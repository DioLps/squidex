﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Security.Claims;
using Squidex.Infrastructure.TestHelpers;

namespace Squidex.Infrastructure.Json;

public class ClaimsPrincipalConverterTests
{
    [Fact]
    public void Should_serialize_and_deserialize()
    {
        var value = new ClaimsPrincipal(
            [
                new ClaimsIdentity(
                    [
                        new Claim("email", "me@email.com"),
                        new Claim("username", "me@email.com"),
                    ],
                    "Cookie"),
                new ClaimsIdentity(
                    [
                        new Claim("user_id", "12345"),
                        new Claim("login", "me"),
                    ],
                    "Google"),
            ]);

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Equal(value.Identities.ElementAt(0).AuthenticationType, serialized.Identities.ElementAt(0).AuthenticationType);
        Assert.Equal(value.Identities.ElementAt(1).AuthenticationType, serialized.Identities.ElementAt(1).AuthenticationType);
    }

    [Fact]
    public void Should_serialize_and_deserialize_null_principal()
    {
        ClaimsPrincipal? value = null;

        var serialized = value.SerializeAndDeserializeJson();

        Assert.Null(serialized);
    }
}
