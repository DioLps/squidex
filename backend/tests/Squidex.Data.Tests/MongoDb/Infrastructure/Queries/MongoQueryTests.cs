﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NodaTime;
using NodaTime.Text;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Queries;
using Squidex.MongoDb.TestHelpers;
using ClrFilter = Squidex.Infrastructure.Queries.ClrFilter;

namespace Squidex.MongoDb.Infrastructure.Queries;

public class MongoQueryTests
{
    public sealed class TestEntity
    {
        public DomainId Id { get; set; }

        public Instant Created { get; set; }

        public RefToken CreatedBy { get; set; }

        public string Text { get; set; }

        public long Version { get; set; }
    }

    static MongoQueryTests()
    {
        MongoTestUtils.SetupBson();
    }

    [Fact]
    public void Should_not_throw_exception_for_invalid_field()
    {
        var filter = ClrFilter.Eq("invalid", "Value");

        AssertQuery("{ 'invalid' : 'Value' }", filter);
    }

    [Fact]
    public void Should_make_query_with_id_guid()
    {
        var id = Guid.NewGuid();

        var filter = ClrFilter.Eq("Id", id);

        AssertQuery("{ '_id' : '[value]' }", filter, id);
    }

    [Fact]
    public void Should_make_query_with_id_string()
    {
        var id = DomainId.NewGuid().ToString();

        var filter = ClrFilter.Eq("Id", id);

        AssertQuery("{ '_id' : '[value]' }", filter, id);
    }

    [Fact]
    public void Should_make_query_with_id_guid_list()
    {
        var id = Guid.NewGuid();

        var filter = ClrFilter.In("Id", new List<Guid> { id });

        AssertQuery("{ '_id' : { '$in' : ['[value]'] } }", filter, id);
    }

    [Fact]
    public void Should_make_query_with_id_string_list()
    {
        var id = DomainId.NewGuid().ToString();

        var filter = ClrFilter.In("Id", new List<string> { id });

        AssertQuery("{ '_id' : { '$in' : ['[value]'] } }", filter, id);
    }

    [Fact]
    public void Should_make_query_with_instant()
    {
        var time = "1988-01-19T12:00:00Z";

        var filter = ClrFilter.Eq("Version", InstantPattern.ExtendedIso.Parse(time).Value);

        AssertQuery("{ 'Version' : ISODate('[value]') }", filter, time);
    }

    [Fact]
    public void Should_make_query_with_reftoken()
    {
        var filter = ClrFilter.Eq("CreatedBy", "subject:me");

        AssertQuery("{ 'CreatedBy' : 'subject:me' }", filter);
    }

    [Fact]
    public void Should_make_query_with_reftoken_cleanup()
    {
        var filter = ClrFilter.Eq("CreatedBy", "me");

        AssertQuery("{ 'CreatedBy' : 'subject:me' }", filter);
    }

    [Fact]
    public void Should_make_query_with_reftoken_fix()
    {
        var filter = ClrFilter.Eq("CreatedBy", "user:me");

        AssertQuery("{ 'CreatedBy' : 'subject:me' }", filter);
    }

    [Fact]
    public void Should_make_query_with_number()
    {
        var filter = ClrFilter.Eq("Version", 0L);

        AssertQuery("{ 'Version' : NumberLong(0) }", filter);
    }

    [Fact]
    public void Should_make_query_with_number_and_list()
    {
        var filter = ClrFilter.In("Version", new List<long> { 0L, 2L, 5L });

        AssertQuery("{ 'Version' : { '$in' : [NumberLong(0), NumberLong(2), NumberLong(5)] } }", filter);
    }

    [Fact]
    public void Should_make_query_with_contains_and_null_value()
    {
        var filter = ClrFilter.Contains("Text", null!);

        AssertQuery("{ 'Text' : /null/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_contains()
    {
        var filter = ClrFilter.Contains("Text", "search");

        AssertQuery("{ 'Text' : /search/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_contains_and_invalid_character()
    {
        var filter = ClrFilter.Contains("Text", "search(");

        AssertQuery("{ 'Text' : /search\\(/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_endswith_and_null_value()
    {
        var filter = ClrFilter.EndsWith("Text", null!);

        AssertQuery("{ 'Text' : /null$/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_endswith()
    {
        var filter = ClrFilter.EndsWith("Text", "search");

        AssertQuery("{ 'Text' : /search$/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_endswithand_invalid_character()
    {
        var filter = ClrFilter.EndsWith("Text", "search(");

        AssertQuery("{ 'Text' : /search\\($/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_startswith_and_null_value()
    {
        var filter = ClrFilter.StartsWith("Text", null!);

        AssertQuery("{ 'Text' : /^null/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_startswith()
    {
        var filter = ClrFilter.StartsWith("Text", "search");

        AssertQuery("{ 'Text' : /^search/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_startswith_and_invalid_character()
    {
        var filter = ClrFilter.StartsWith("Text", "search(");

        AssertQuery("{ 'Text' : /^search\\(/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_matchs_and_null_value()
    {
        var filter = ClrFilter.Matchs("Text", null!);

        AssertQuery("{ 'Text' : /null/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_matchs()
    {
        var filter = ClrFilter.Matchs("Text", "^search$");

        AssertQuery("{ 'Text' : /^search$/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_matchs_and_regex_syntax()
    {
        var filter = ClrFilter.Matchs("Text", "/search/i");

        AssertQuery("{ 'Text' : /search/i }", filter);
    }

    [Fact]
    public void Should_make_query_with_matchs_and_regex_case_sensitive_syntax()
    {
        var filter = ClrFilter.Matchs("Text", "/search/");

        AssertQuery("{ 'Text' : /search/ }", filter);
    }

    [Fact]
    public void Should_make_query_with_empty_for_class()
    {
        var filter = ClrFilter.Empty("Text");

        AssertQuery("{ '$or' : [{ 'Text' : { '$exists' : false } }, { 'Text' : null }, { 'Text' : '' }, { 'Text' : { '$size' : 0 } }] }", filter);
    }

    [Fact]
    public void Should_make_query_with_exists()
    {
        var filter = ClrFilter.Exists("Text");

        AssertQuery("{ 'Text' : { '$exists' : true, '$ne' : null } }", filter);
    }

    [Fact]
    public void Should_make_query_with_and()
    {
        var filter = ClrFilter.And(ClrFilter.Eq("A", 1), ClrFilter.Eq("B", 2));

        AssertQuery("{ 'A' : 1, 'B' : 2 }", filter);
    }

    [Fact]
    public void Should_make_query_with_or()
    {
        var filter = ClrFilter.Or(ClrFilter.Eq("A", 1), ClrFilter.Eq("B", 2));

        AssertQuery("{ '$or' : [{ 'A' : 1 }, { 'B' : 2 }] }", filter);
    }

    [Fact]
    public void Should_make_query_with_not()
    {
        var filter = ClrFilter.Not(ClrFilter.Lt("A", 1));

        AssertQuery("{ 'A' : { '$not' : { '$lt' : 1 } } }", filter);
    }

    [Fact]
    public void Should_make_query_with_full_text()
    {
        var query = new ClrQuery { FullText = "Hello my World" };

        AssertQuery(query, "{ '$text' : { '$search' : 'Hello my World' } }");
    }

    [Fact]
    public void Should_make_orderby_with_single_field()
    {
        var sorting = SortBuilder.Descending("Number");

        AssertSorting("{ 'Number' : -1 }", sorting);
    }

    [Fact]
    public void Should_make_orderby_with_multiple_fields()
    {
        var sorting1 = SortBuilder.Ascending("Number");
        var sorting2 = SortBuilder.Descending("Text");

        AssertSorting("{ 'Number' : 1, 'Text' : -1 }", sorting1, sorting2);
    }

    [Fact]
    public void Should_make_take_statement()
    {
        var query = new ClrQuery { Take = 3 };

        var cursor = A.Fake<IFindFluent<TestEntity, TestEntity>>();

        cursor.QueryLimit(query);

        A.CallTo(() => cursor.Limit(3))
            .MustHaveHappened();
    }

    [Fact]
    public void Should_make_skip_statement()
    {
        var query = new ClrQuery { Skip = 3 };

        var cursor = A.Fake<IFindFluent<TestEntity, TestEntity>>();

        cursor.QuerySkip(query);

        A.CallTo(() => cursor.Skip(3))
            .MustHaveHappened();
    }

    private static void AssertQuery(string expected, FilterNode<ClrValue> filter, object? arg = null)
    {
        AssertQuery(new ClrQuery { Filter = filter }, expected, arg);
    }

    private static void AssertQuery(ClrQuery query, string expected, object? arg = null)
    {
        var filter = query.BuildFilter<TestEntity>().Filter!;

        var rendered =
            filter.Render(
                new RenderArgs<TestEntity>(
                    BsonSerializer.SerializerRegistry.GetSerializer<TestEntity>(),
                    BsonSerializer.SerializerRegistry))
            .ToString();

        Assert.Equal(Cleanup(expected, arg), rendered);
    }

    private static void AssertSorting(string expected, params SortNode[] sort)
    {
        var cursor = A.Fake<IFindFluent<TestEntity, TestEntity>>();

        var rendered = string.Empty;

        A.CallTo(() => cursor.Sort(A<SortDefinition<TestEntity>>._))
            .Invokes((SortDefinition<TestEntity> sortDefinition) =>
            {
                rendered =
                    sortDefinition.Render(
                        new RenderArgs<TestEntity>(
                           BsonSerializer.SerializerRegistry.GetSerializer<TestEntity>(),
                           BsonSerializer.SerializerRegistry))
                   .ToString();
            });

        cursor.QuerySort(new ClrQuery { Sort = sort.ToList() });

        Assert.Equal(Cleanup(expected), rendered);
    }

    private static string Cleanup(string filter, object? arg = null)
    {
        return filter.Replace('\'', '"').Replace("[value]", arg?.ToString(), StringComparison.Ordinal);
    }
}
