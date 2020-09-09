using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

using NUnit.Framework;

using Woof.WebSocket;

namespace Tests {
    
    public class Tests {
        
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void TestBufferWriterTest() {
            var w1 = new TestBufferWriter(10);
            var buffers = new ArraySegment<byte>[4];
            for (var i = 0; i < 4; i++) buffers[i] = new ArraySegment<byte>(new byte[3]);
            int bytesRead;
            for (var i = 0; i < 4; i++) {
                bytesRead = w1.Write(buffers[i], out var isMessageEnd);
                if (i < 3) {
                    Assert.AreEqual(3, bytesRead, "Invalid return value");
                    Assert.IsFalse(isMessageEnd, "Invalid message end indication");
                }
                else {
                    Assert.AreEqual(1, bytesRead, "Invalid return value");
                    Assert.IsTrue(isMessageEnd, "Invalid message end indication");
                }
            }
            var s = w1.Data.AsSpan();
            var b1 = buffers[0].AsSpan();
            var b2 = buffers[1].AsSpan();
            var b3 = buffers[2].AsSpan();
            var b4 = buffers[3].AsSpan();
            Assert.IsTrue(s.Slice(0, 3).SequenceEqual(b1), "Buffer content invalid.");
            Assert.IsTrue(s.Slice(3, 3).SequenceEqual(b2), "Buffer content invalid.");
            Assert.IsTrue(s.Slice(6, 3).SequenceEqual(b3), "Buffer content invalid.");
            Assert.IsTrue(s.Slice(9, 1).SequenceEqual(b4.Slice(0, 1)), "Buffer content invalid.");
        }

        [Test]
        public void TestDynamicBuffer() {
            int bytesRead;
            var w = new TestBufferWriter(0);
            var b = new DynamicBuffer(1);
            bytesRead = w.Write(b, out var isMessageEnd);
            b.Advance(bytesRead, isMessageEnd);
            Assert.AreEqual(0, bytesRead, "Invalid return value");
            Assert.IsTrue(isMessageEnd, "Invalid message end indication");
            Assert.AreEqual(0, b.Count, "Invalid buffer count after Advance()");
            for (int i = 0; i < 255; i++) {
                w = new TestBufferWriter(i);
                b = new DynamicBuffer(10);
                isMessageEnd = false;
                while (!isMessageEnd) {
                    bytesRead = w.Write(b, out isMessageEnd);
                    b.Advance(bytesRead, isMessageEnd);
                }
                Assert.AreEqual(0, b.Offset, "Invalid buffer offset after Advance()");
                Assert.AreEqual(i, b.Count, "Invalid buffer count after Advance()");
                var s1 = w.Data.AsSpan();
                var s2 = (Span<byte>)(ArraySegment<byte>)b;
                Assert.IsTrue(s1.SequenceEqual(s2));
            }
        }
    
        [Test]
        public void GetKeyTest() {
            var codec = new WoofCodec();
            var apiKey = codec.GetKey();
            var secret = codec.GetKey();
            var hash = codec.GetHash(apiKey);
            var b = new StringBuilder();
            b.AppendLine($"    Key: {codec.GetKeyString(apiKey)}");
            b.AppendLine($" Secret: {codec.GetKeyString(secret)}");
            b.AppendLine($"   Hash: {codec.GetKeyString(hash)}");
            Console.WriteLine(b.ToString());
        }

    }

}