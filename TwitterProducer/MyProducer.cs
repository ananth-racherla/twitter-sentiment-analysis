using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitterProducer {
    /// <summary>
    /// https://github.com/confluentinc/confluent-kafka-dotnet/blob/0.11.6.x/test/Confluent.Kafka.Benchmark/BenchmarkProducer.cs
    /// </summary>
    public class MyProducer : IDisposable {
        Logger log = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();
        Producer<string, string> Producer { get; }
        public string Topic { get; }

        public MyProducer(string topic, string bootStrapServer)
        {
            var config = new Dictionary<string, object>(){
                { "bootstrap.servers", bootStrapServer },
                {"request.required.acks", -1},
                {"compression.type", "snappy" },
                {"linger.ms", "20" },
                //{"batch.size", (32*1024).ToString() },
            };

            Producer = new Producer<string, string>(config, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8));
            Producer.OnError += OnError;

            Topic = topic;
        }


        private void OnError(object sender, Error e)
        {
            log.Error($"Failed to send message {e.Reason}");
        }

        public Task Produce(string key, string value)
        {
            return Producer.ProduceAsync(Topic, key, value)
            .ContinueWith(result => {
                var msg = result.Result;
                if (msg.Error.Code != ErrorCode.NoError) {
                    log.Error($"failed to deliver message: {msg.Error.Reason}");
                } else {
                    log.Information($"delivered to: {result.Result.TopicPartitionOffset}");
                }
            });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue) {
                if (disposing) {
                    Producer.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MyProducer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
