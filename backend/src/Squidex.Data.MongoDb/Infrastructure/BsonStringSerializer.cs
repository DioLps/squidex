﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.ComponentModel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Squidex.Infrastructure;

public sealed class BsonStringSerializer<T> : SerializerBase<T>
{
    private static readonly BsonStringSerializer<T> Instance = new BsonStringSerializer<T>();
    private readonly TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(T));

    public static void Register()
    {
        BsonSerializer.TryRegisterSerializer(Instance);
    }

    private BsonStringSerializer()
    {
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.CurrentBsonType == BsonType.Null)
        {
            context.Reader.ReadNull();

            return default!;
        }
        else
        {
            var value = context.Reader.ReadString();

            return (T)typeConverter.ConvertFromInvariantString(value)!;
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        var text = value?.ToString();

        if (text != null)
        {
            context.Writer.WriteString(text);
        }
        else
        {
            context.Writer.WriteNull();
        }
    }
}
