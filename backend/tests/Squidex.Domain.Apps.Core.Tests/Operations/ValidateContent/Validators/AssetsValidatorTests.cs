﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Core.ValidateContent;
using Squidex.Domain.Apps.Core.ValidateContent.Validators;
using Squidex.Infrastructure.Collections;
using Xunit;

namespace Squidex.Domain.Apps.Core.Operations.ValidateContent.Validators
{
    public class AssetsValidatorTests
    {
        private readonly List<string> errors = new List<string>();

        public sealed class AssetInfo : IAssetInfo
        {
            public Guid AssetId { get; set; }

            public string FileName { get; set; }

            public string FileHash { get; set; }

            public string Slug { get; set; }

            public long FileSize { get; set; }

            public bool IsImage { get; set; }

            public int? PixelWidth { get; set; }

            public int? PixelHeight { get; set; }

            public AssetMetadata Metadata { get; set; }

            public AssetType Type { get; set; }
        }

        private readonly AssetInfo document = new AssetInfo
        {
            AssetId = Guid.NewGuid(),
            FileName = "MyDocument.pdf",
            FileSize = 1024 * 4,
            Type = AssetType.Unknown
        };

        private readonly AssetInfo image1 = new AssetInfo
        {
            AssetId = Guid.NewGuid(),
            FileName = "MyImage.png",
            FileSize = 1024 * 8,
            Type = AssetType.Image,
            Metadata =
                new AssetMetadata()
                    .SetPixelWidth(800)
                    .SetPixelHeight(600)
        };

        private readonly AssetInfo image2 = new AssetInfo
        {
            AssetId = Guid.NewGuid(),
            FileName = "MyImage.png",
            FileSize = 1024 * 8,
            Type = AssetType.Image,
            Metadata =
                new AssetMetadata()
                    .SetPixelWidth(800)
                    .SetPixelHeight(600)
        };

        [Fact]
        public async Task Should_not_add_error_if_assets_are_valid()
        {
            var sut = Validator(new AssetsFieldProperties());

            await sut.ValidateAsync(CreateValue(document.AssetId), errors);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task Should_add_error_if_asset_are_not_valid()
        {
            var assetId = Guid.NewGuid();

            var sut = Validator(new AssetsFieldProperties());

            await sut.ValidateAsync(CreateValue(assetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { $"[1]: Id '{assetId}' not found." });
        }

        [Fact]
        public async Task Should_not_add_error_if_asset_are_not_valid_but_in_optimized_mode()
        {
            var assetId = Guid.NewGuid();

            var sut = Validator(new AssetsFieldProperties());

            await sut.ValidateAsync(CreateValue(assetId), errors, updater: c => c.Optimized());

            Assert.Empty(errors);
        }

        [Fact]
        public async Task Should_add_error_if_document_is_too_small()
        {
            var sut = Validator(new AssetsFieldProperties { MinSize = 5 * 1024 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[1]: \'4 kB\' less than minimum of \'5 kB\'." });
        }

        [Fact]
        public async Task Should_add_error_if_document_is_too_big()
        {
            var sut = Validator(new AssetsFieldProperties { MaxSize = 5 * 1024 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: \'8 kB\' greater than maximum of \'5 kB\'." });
        }

        [Fact]
        public async Task Should_add_error_if_document_is_not_an_image()
        {
            var sut = Validator(new AssetsFieldProperties { MustBeImage = true });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[1]: Not an image." });
        }

        [Fact]
        public async Task Should_add_error_if_image_width_is_too_small()
        {
            var sut = Validator(new AssetsFieldProperties { MinWidth = 1000 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: Width \'800px\' less than minimum of \'1000px\'." });
        }

        [Fact]
        public async Task Should_add_error_if_image_width_is_too_big()
        {
            var sut = Validator(new AssetsFieldProperties { MaxWidth = 700 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: Width \'800px\' greater than maximum of \'700px\'." });
        }

        [Fact]
        public async Task Should_add_error_if_image_height_is_too_small()
        {
            var sut = Validator(new AssetsFieldProperties { MinHeight = 800 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: Height \'600px\' less than minimum of \'800px\'." });
        }

        [Fact]
        public async Task Should_add_error_if_image_height_is_too_big()
        {
            var sut = Validator(new AssetsFieldProperties { MaxHeight = 500 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: Height \'600px\' greater than maximum of \'500px\'." });
        }

        [Fact]
        public async Task Should_add_error_if_image_has_invalid_aspect_ratio()
        {
            var sut = Validator(new AssetsFieldProperties { AspectWidth = 1, AspectHeight = 1 });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[] { "[2]: Aspect ratio not '1:1'." });
        }

        [Fact]
        public async Task Should_add_error_if_image_has_invalid_extension()
        {
            var sut = Validator(new AssetsFieldProperties { AllowedExtensions = ReadOnlyCollection.Create("mp4") });

            await sut.ValidateAsync(CreateValue(document.AssetId, image1.AssetId), errors);

            errors.Should().BeEquivalentTo(
                new[]
                {
                    "[1]: Invalid file extension.",
                    "[2]: Invalid file extension."
                });
        }

        private static object CreateValue(params Guid[] ids)
        {
            return ids.ToList();
        }

        private IValidator Validator(AssetsFieldProperties properties)
        {
            return new AssetsValidator(properties, new CheckAssets(ids =>
            {
                return Task.FromResult<IReadOnlyList<IAssetInfo>>(new List<IAssetInfo> { document, image1, image2 });
            }));
        }
    }
}
