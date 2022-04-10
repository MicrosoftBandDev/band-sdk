// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.MemoryPipeStream
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Band.Admin
{
    internal class MemoryPipeStream : Stream
    {
        private static readonly Task completeTask = Task.Delay(0);
        private readonly int capacity;
        private readonly ProgressTrackerPrimitive progress;
        private readonly ILoggerProvider loggerProvider;
        private readonly BufferPool bufferPool;
        private readonly LinkedList<PooledBuffer> buffers;
        private readonly SemaphoreSlim readSatisfied;
        private int readPos;
        private int writePos;
        private int currentReadBufferOffset;
        private int currentWriteBufferOffset;
        private bool isEof;
        private bool isDisposed;
        private byte[] pendingReadBuffer;
        private int pendingReadBufferOffset;
        private int pendingReadBufferByteCount;

        public MemoryPipeStream(
          int capacity,
          ProgressTrackerPrimitive progressTracker,
          ILoggerProvider loggerProvider)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            if (progressTracker == null)
                throw new ArgumentNullException(nameof(progressTracker));
            if (loggerProvider == null)
                throw new ArgumentNullException(nameof(loggerProvider));
            this.capacity = capacity;
            this.progress = progressTracker;
            this.loggerProvider = loggerProvider;
            this.bufferPool = BufferServer.GetPool(Math.Min(capacity, 8192));
            this.buffers = new LinkedList<PooledBuffer>();
            this.readSatisfied = new SemaphoreSlim(0, 1);
        }

        private object SyncRoot => (object)this.buffers;

        public override bool CanRead
        {
            get
            {
                this.CheckIfDisposed();
                return !this.isEof;
            }
        }

        public override bool CanWrite
        {
            get
            {
                this.CheckIfDisposed();
                return !this.isEof;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                this.CheckIfDisposed();
                return false;
            }
        }

        public override bool CanSeek
        {
            get
            {
                this.CheckIfDisposed();
                return true;
            }
        }

        public override long Length
        {
            get
            {
                this.CheckIfDisposed();
                return (long)this.capacity;
            }
        }

        public override long Position
        {
            get
            {
                this.CheckIfDisposed();
                this.CheckIfEof(false);
                return (long)this.readPos;
            }
            set => throw new InvalidOperationException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int num = this.ReadNonBlocking(buffer, offset, count);
            if (num > 0)
                return num;
            if (num == -1)
                return 0;
            try
            {
                this.readSatisfied.Wait();
            }
            catch (ArgumentNullException ex)
            {
            }
            this.CheckIfEof(false);
            this.CheckIfDisposed();
            return this.pendingReadBufferByteCount;
        }

        public override Task<int> ReadAsync(
          byte[] buffer,
          int offset,
          int count,
          CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int val1 = this.ReadNonBlocking(buffer, offset, count);
            return val1 != 0 ? Task.FromResult<int>(Math.Max(val1, 0)) : this.WaitForDataCopiedAsync(cancellationToken);
        }

        private async Task<int> WaitForDataCopiedAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.readSatisfied.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                lock (this.SyncRoot)
                {
                    if (this.pendingReadBuffer != null)
                    {
                        this.pendingReadBuffer = (byte[])null;
                        this.pendingReadBufferOffset = 0;
                        this.pendingReadBufferByteCount = 0;
                        throw;
                    }
                }
            }
            this.CheckIfEof(false);
            this.CheckIfDisposed();
            return this.pendingReadBufferByteCount;
        }

        private int ReadNonBlocking(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            this.CheckIfDisposed();
            if (this.readPos + count > this.capacity)
                count = this.capacity - this.readPos;
            if (count == 0)
                return -1;
            int num1 = 0;
            lock (this.SyncRoot)
            {
                this.CheckIfEof(false);
                if (this.readPos == this.writePos)
                {
                    this.pendingReadBuffer = buffer;
                    this.pendingReadBufferOffset = offset;
                    this.pendingReadBufferByteCount = count;
                }
                else
                {
                    count = Math.Min(count, this.writePos - this.readPos);
                    while (count > 0)
                    {
                        int num2 = Math.Min(count, this.bufferPool.BufferSize - this.currentReadBufferOffset);
                        Array.Copy((Array)this.buffers.First.Value.Buffer, this.currentReadBufferOffset, (Array)buffer, offset, num2);
                        this.currentReadBufferOffset += num2;
                        this.readPos += num2;
                        offset += num2;
                        count -= num2;
                        num1 += num2;
                        this.progress.AddStepsCompleted(num2);
                        if (this.currentReadBufferOffset == this.bufferPool.BufferSize)
                        {
                            this.currentReadBufferOffset = 0;
                            this.buffers.First.Value.Dispose();
                            this.buffers.RemoveFirst();
                        }
                    }
                }
            }
            return num1;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));
            this.CheckIfDisposed();
            if (this.writePos + count > this.capacity)
                throw new EndOfStreamException("Attempted to write past the end of the stream");
            lock (this.SyncRoot)
            {
                this.CheckIfEof(true);
                if (this.pendingReadBuffer != null)
                {
                    int num = Math.Min(count, this.pendingReadBufferByteCount);
                    Array.Copy((Array)buffer, offset, (Array)this.pendingReadBuffer, this.pendingReadBufferOffset, num);
                    this.writePos += num;
                    this.readPos += num;
                    offset += num;
                    count -= num;
                    this.pendingReadBuffer = (byte[])null;
                    this.pendingReadBufferOffset = 0;
                    this.pendingReadBufferByteCount = num;
                    this.progress.AddStepsCompleted(num);
                    if (this.readSatisfied.CurrentCount == 0)
                        this.readSatisfied.Release();
                }
                while (count > 0)
                {
                    if (this.buffers.Count == 0 || this.currentWriteBufferOffset == this.bufferPool.BufferSize)
                    {
                        this.currentWriteBufferOffset = 0;
                        this.buffers.AddLast(this.bufferPool.GetBuffer());
                    }
                    int num = Math.Min(count, this.bufferPool.BufferSize - this.currentWriteBufferOffset);
                    Array.Copy((Array)buffer, offset, (Array)this.buffers.Last.Value.Buffer, this.currentWriteBufferOffset, num);
                    this.writePos += num;
                    this.currentWriteBufferOffset += num;
                    count -= num;
                    offset += num;
                    this.progress.AddStepsCompleted(num);
                }
            }
        }

        public override Task WriteAsync(
          byte[] buffer,
          int offset,
          int count,
          CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.Write(buffer, offset, count);
            return MemoryPipeStream.completeTask;
        }

        public void SetEndOfStream()
        {
            this.CheckIfDisposed();
            lock (this.SyncRoot)
            {
                this.SetEndOfStreamLocked();
                if (this.readSatisfied.CurrentCount != 0)
                    return;
                this.readSatisfied.Release();
            }
        }

        public override void Flush()
        {
            this.CheckIfDisposed();
            this.CheckIfEof(true);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.CheckIfDisposed();
            this.CheckIfEof(true);
            return MemoryPipeStream.completeTask;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new InvalidOperationException();

        public override void SetLength(long value) => throw new InvalidOperationException();

        protected override void Dispose(bool disposing)
        {
            if (!disposing || this.isDisposed)
                return;
            lock (this.SyncRoot)
            {
                if (!this.isDisposed)
                {
                    this.isDisposed = true;
                    this.SetEndOfStreamLocked();
                    if (this.readSatisfied.CurrentCount == 0)
                        this.readSatisfied.Release();
                    this.readSatisfied.Dispose();
                    foreach (PooledBuffer buffer in this.buffers)
                        buffer.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void CheckIfEof(bool write)
        {
            if (this.isEof)
                throw new EndOfStreamException(write ? "Attempted to write past the end of the stream" : "Attempted to read past the end of the stream");
        }

        private void CheckIfDisposed()
        {
            if (this.isDisposed)
                throw new ObjectDisposedException(nameof(MemoryPipeStream));
        }

        private void SetEndOfStreamLocked()
        {
            if (this.isEof)
                return;
            this.isEof = true;
            this.pendingReadBufferByteCount = 0;
            this.pendingReadBuffer = (byte[])null;
        }
    }
}
