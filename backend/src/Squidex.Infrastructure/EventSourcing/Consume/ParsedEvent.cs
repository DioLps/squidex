﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
#pragma warning disable MA0048 // File name must match type name

using Squidex.Events;

namespace Squidex.Infrastructure.EventSourcing.Consume;

public record struct ParsedEvent(Envelope<IEvent>? Event, StreamPosition Position);

public record struct ParsedEvents(List<Envelope<IEvent>> Events, StreamPosition Position);
