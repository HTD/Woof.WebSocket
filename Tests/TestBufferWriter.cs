using System;

namespace Tests {

    class TestBufferWriter {

        public byte[] Data { get; }

        public TestBufferWriter(int sampleSize) {
            Data = new byte[sampleSize];
            PRNG.NextBytes(Data);
            Count = sampleSize;
        }

        public int Write(ArraySegment<byte> buffer, out bool isMessageEnd) {
            var count = buffer.Count < Count ? buffer.Count : Count;
            Buffer.BlockCopy(Data, Offset, buffer.Array, buffer.Offset, count);
            Offset += count;
            Count -= count;
            isMessageEnd = Count < 1;
            return count;
        }

        public void Reset() {
            Offset = 0;
            Count = Data.Length;
        }

        private static readonly Random PRNG = new Random();
        private int Offset;
        private int Count;

    }

}