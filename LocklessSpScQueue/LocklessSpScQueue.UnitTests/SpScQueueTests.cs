using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using Microsoft.Concurrency.TestTools.UnitTesting.Chess;

namespace LocklessSpScQueue.UnitTests {
    [TestClass]
    public class SpScQueueTests {

        [TestMethod]
        [ChessTestMethod]
        public void SingleThreaded() {
            var q = new SpScQueue<int>();

            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);

            Assert.AreEqual(1, q.Dequeue());
            Assert.AreEqual(2, q.Dequeue());
            Assert.AreEqual(3, q.Dequeue());
            Assert.AreEqual(0, q.Dequeue());
        }

        [TestMethod]
        [ChessTestMethod]
        public void OneReaderOneWriterThreads() {
            var q = new SpScQueue<int>();

            var writer = new Thread(() => {
                for (var i = 1; i <= 1000; i++)
                    q.Enqueue(i);
            });

            var reader = new Thread(() => {
                for (var i = 1; i <= 1000; i++) {
                    int item;
                    while (!q.TryDequeue(out item)) { }

                    Assert.AreEqual(i, item);
                }
            });

            writer.Start();
            reader.Start();

            reader.Join();
            writer.Join();
        }

        [TestMethod]
        [ChessTestMethod]
        public void PerformanceTest() {
            const int n = 10000000;

            var q = new SpScQueue<int>();

            // Warm up the queue
            for (var i = 0; i < 10000; i++)
                q.Enqueue(0);

            for (var i = 0; i < 10000; i++)
                q.Dequeue();

            var writer = new Thread(() => {
                for (var i = 1; i <= n; i++)
                    q.Enqueue(i);
            });

            var reader = new Thread(() => {
                for (var i = 1; i <= n; i++) {
                    int item;
                    while (!q.TryDequeue(out item)) { }

                    Assert.AreEqual(i, item);
                }
            });

            var start = DateTime.Now;

            writer.Start();
            reader.Start();

            reader.Join();
            writer.Join();

            var end = DateTime.Now;

            Console.WriteLine(end - start);
        }
    }
}
