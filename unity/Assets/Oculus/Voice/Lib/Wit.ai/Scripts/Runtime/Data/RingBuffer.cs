/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Events;
using UnityEngine;

namespace Facebook.WitAi.Data
{
    public class RingBuffer<T>
    {
        public delegate void OnDataAdded(T[] data, int offset, int length);
        public delegate void ByteDataWriter(T[] buffer, int offset, int length);


        public OnDataAdded OnDataAddedEvent;

        private readonly T[] buffer;
        private int bufferIndex;
        private long bufferDataLength;
        public int Capacity => buffer.Length;

        public int GetBufferArrayIndex(long bufferDataIndex)
        {
            if (bufferDataLength <= bufferDataIndex) return -1;
            if (bufferDataLength - bufferDataIndex > buffer.Length) return -1;

            var endOffset = bufferDataLength - bufferDataIndex;
            var index = bufferIndex - endOffset;
            if (index < 0) index = buffer.Length + index;
            return (int) index;
        }

        public T this[long bufferDataIndex] => buffer[GetBufferArrayIndex(bufferDataIndex)];

        public void Clear(bool eraseData = false)
        {
            bufferIndex = 0;
            bufferDataLength = 0;

            if (eraseData)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = default;
                }
            }
        }

        public class Marker
        {
            private long bufferDataIndex;
            private int index;
            private readonly RingBuffer<T> ringBuffer;

            public RingBuffer<T> RingBuffer => ringBuffer;

            public Marker(RingBuffer<T> ringBuffer, long markerPosition, int bufIndex)
            {
                this.ringBuffer = ringBuffer;
                bufferDataIndex = markerPosition;
                index = bufIndex;
            }

            public bool IsValid => ringBuffer.bufferDataLength - bufferDataIndex <= ringBuffer.Capacity;
            public long AvailableByteCount => Math.Min(ringBuffer.Capacity, RequestedByteCount);
            public long RequestedByteCount => ringBuffer.bufferDataLength - bufferDataIndex;
            public long CurrentBufferDataIndex => bufferDataIndex;

            public int Read(T[] buffer, int offset, int length, bool skipToNextValid = false)
            {
                int read = -1;
                if (!IsValid && skipToNextValid && ringBuffer.bufferDataLength > ringBuffer.Capacity)
                {
                    bufferDataIndex = ringBuffer.bufferDataLength - ringBuffer.Capacity;
                }

                if (IsValid)
                {
                    read = this.ringBuffer.Read(buffer, offset, length, bufferDataIndex);
                    bufferDataIndex += read;
                    index += read;
                    if (index > buffer.Length) index -= buffer.Length;
                }


                return read;
            }

            public void ReadIntoWriters(params ByteDataWriter[] writers)
            {
                if (!IsValid && ringBuffer.bufferDataLength > ringBuffer.Capacity)
                {
                    bufferDataIndex = ringBuffer.bufferDataLength - ringBuffer.Capacity;
                }

                index = ringBuffer.GetBufferArrayIndex(bufferDataIndex);
                var length = (int) (ringBuffer.bufferDataLength - bufferDataIndex);
                if (IsValid && length > 0)
                {
                    for (int i = 0; i < writers.Length; i++)
                    {
                        ringBuffer.WriteFromBuffer(writers[i], index, length);
                    }
                }

                bufferDataIndex += length;
                index = ringBuffer.GetBufferArrayIndex(bufferDataIndex);
            }

            public Marker Clone()
            {
                return new Marker(ringBuffer, bufferDataIndex, index);
            }

            public void Offset(int amount)
            {
                bufferDataIndex += amount;
                if (bufferDataIndex < 0) bufferDataIndex = 0;
                if (bufferDataIndex > ringBuffer.bufferDataLength)
                {
                    bufferDataIndex = ringBuffer.bufferDataLength;
                }

                index = ringBuffer.GetBufferArrayIndex(bufferDataIndex);
            }
        }

        public RingBuffer(int capacity)
        {
            buffer = new T[capacity];
        }

        private int CopyToBuffer(T[] data, int offset, int length, int bufferIndex)
        {
            if (length > buffer.Length)
                throw new ArgumentException(
                    "Push data exceeds buffer size.");

            if (bufferIndex + length < buffer.Length)
            {
                Array.Copy(data, offset, buffer, bufferIndex, length);
                return bufferIndex + length;
            }
            else
            {
                int len = Mathf.Min(length, buffer.Length);
                int endChunkLength = buffer.Length - bufferIndex;
                int wrappedChunkLength = len - endChunkLength;
                try
                {

                    Array.Copy(data, offset, buffer, bufferIndex, endChunkLength);
                    Array.Copy(data, offset + endChunkLength, buffer, 0, wrappedChunkLength);
                    return wrappedChunkLength;
                }
                catch (ArgumentException e)
                {
                    throw e;
                }
            }
        }

        public void WriteFromBuffer(ByteDataWriter writer, long bufferIndex, int length)
        {
            lock (buffer)
            {
                if (bufferIndex + length < buffer.Length)
                {
                    writer(buffer, (int) bufferIndex, length);
                }
                else
                {
                    if (length > bufferDataLength)
                    {
                        length = (int) (bufferDataLength - bufferIndex);
                    }

                    if (length > buffer.Length)
                    {
                        length = buffer.Length;
                    }

                    var l = Math.Min(buffer.Length, length);
                    int endChunkLength = (int) (buffer.Length - bufferIndex);
                    int wrappedChunkLength = l - endChunkLength;

                    writer(buffer, (int) bufferIndex, endChunkLength);
                    writer(buffer, 0, wrappedChunkLength);
                }
            }
        }

        private int CopyFromBuffer(T[] data, int offset, int length, int bufferIndex)
        {
            if (length > buffer.Length)
                throw new ArgumentException(
                    $"Push data exceeds buffer size {length} < {buffer.Length}" );

            if (bufferIndex + length < buffer.Length)
            {
                Array.Copy(buffer, bufferIndex, data, offset, length);
                return bufferIndex + length;
            }
            else
            {
                var l = Mathf.Min(buffer.Length, length);
                int endChunkLength = buffer.Length - bufferIndex;
                int wrappedChunkLength = l - endChunkLength;

                Array.Copy(buffer, bufferIndex, data, offset, endChunkLength);
                Array.Copy(buffer, 0, data, offset + endChunkLength, wrappedChunkLength);
                return wrappedChunkLength;
            }
        }

        public void Push(T[] data, int offset, int length)
        {
            lock (buffer)
            {
                bufferIndex = CopyToBuffer(data, offset, length, bufferIndex);
                bufferDataLength += length;
                OnDataAddedEvent?.Invoke(data, offset, length);
            }
        }

        public void Push(T data)
        {
            lock (buffer)
            {
                buffer[bufferIndex++] = data;
                if (bufferIndex >= buffer.Length)
                {
                    bufferIndex = 0;
                }
                bufferDataLength++;
            }
        }

        public int Read(T[] data, int offset, int length, long bufferDataIndex)
        {
            if (bufferIndex == 0 && bufferDataLength == 0) // The ring buffer has been cleared.
            {
                return 0;
            }

            lock (buffer)
            {
                int read = (int) (Math.Min(bufferDataIndex + length, bufferDataLength) -
                                  bufferDataIndex);

                int bufferIndex = this.bufferIndex - (int) (bufferDataLength - bufferDataIndex);
                if (bufferIndex < 0)
                {
                    bufferIndex = buffer.Length + bufferIndex;
                }

                CopyFromBuffer(data, offset, length, bufferIndex);

                return read;
            }
        }

        public Marker CreateMarker(int offset = 0)
        {
            var markerPosition = bufferDataLength + offset;
            if (markerPosition < 0)
            {
                markerPosition = 0;
            }

            int bufIndex = bufferIndex + offset;
            if (bufIndex < 0)
            {
                bufIndex = buffer.Length + bufIndex;
            }

            if (bufIndex > buffer.Length)
            {
                bufIndex -= buffer.Length;
            }

            var marker = new Marker(this, markerPosition, bufIndex);

            return marker;
        }
    }
}
