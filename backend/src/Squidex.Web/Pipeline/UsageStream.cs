﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

namespace Squidex.Web.Pipeline;

internal sealed class UsageStream(Stream inner) : Stream
{
    private long bytesWritten;

    public long BytesWritten
    {
        get => bytesWritten;
    }

    public override bool CanRead
    {
        get => false;
    }

    public override bool CanSeek
    {
        get => false;
    }

    public override bool CanWrite
    {
        get => inner.CanWrite;
    }

    public override long Length
    {
        get => throw new NotSupportedException();
    }

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
    {
        var result = inner.BeginWrite(buffer, offset, count, callback, state);

        bytesWritten += count;

        return result;
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count,
        CancellationToken cancellationToken)
    {
        await inner.WriteAsync(buffer, offset, count, cancellationToken);

        bytesWritten += count;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);

        bytesWritten += count;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        await inner.WriteAsync(buffer, cancellationToken);

        bytesWritten += buffer.Length;
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        inner.Write(buffer);

        bytesWritten += buffer.Length;
    }

    public override void WriteByte(byte value)
    {
        inner.WriteByte(value);

        bytesWritten++;
    }

    public override Task FlushAsync(
        CancellationToken cancellationToken)
    {
        return inner.FlushAsync(cancellationToken);
    }

    public override void Flush()
    {
        inner.Flush();
    }

    public override void EndWrite(IAsyncResult asyncResult)
    {
        inner.EndWrite(asyncResult);
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }
}
