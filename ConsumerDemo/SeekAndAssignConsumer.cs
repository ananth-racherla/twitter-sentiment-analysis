using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsumerDemo {
    class SeekAndAssignConsumer {
        Logger log = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();

        public string BootstapServer { get; }
        public string Topic { get; }
        public int Partition { get; }
        public long Offset { get; }

        public SeekAndAssignConsumer(string bootstapServer, string topic, int partition, long offset)
        {
            BootstapServer = bootstapServer;
            Topic = topic;
            Partition = partition;
            Offset = offset;
        }


        private void OnMessage(object sender, Message<string, string> message)
        {
            log.Debug($"Received new metadata \n Topic: {message.Topic}\n Partition: {message.Partition}\n Offset: {message.Offset}\n TimeStamp: {message.Timestamp.UtcDateTime.ToLongTimeString()}\n Key: { message.Key}\n Value: {message.Value} ");
        }

        public void Consume(CancellationToken token)
        {
            // the group.id property must be specified when creating a consumer, even 
            // if you do not intend to use any consumer group functionality.

            var config = new Dictionary<string, object>()  {
                { "bootstrap.servers", BootstapServer },
                { "group.id", new Guid().ToString() }
            };

            using (var consumer = new Consumer<string, string>(config, new StringDeserializer(Encoding.UTF8), new StringDeserializer(Encoding.UTF8))) {
                var topicPartition = new TopicPartition(Topic, Partition);

                var topicOffset = new TopicPartitionOffset(topicPartition, new Offset(Offset));

                consumer.Assign(new List<TopicPartitionOffset> { topicOffset });

                //consumer.Seek(topicOffset);

                consumer.OnMessage += OnMessage;

                consumer.OnLog += (_, e) => log.Information("Consumer log:", e);
                consumer.OnPartitionEOF += (_, e) => log.Warning($"End of partition: {e}");
                //consumer.OnPartitionsRevoked += (_, partitions) => log.Debug($"Revoked partitions: [{string.Join(", ", partitions)}]");
                consumer.OnStatistics += (_, json) => log.Debug($"Statistics: {json}");
                consumer.OnError += (_, e) => log.Error($"Error: {e.Reason}");


                while (!token.IsCancellationRequested) {
                    consumer.Poll(100);
                }
            }
            log.Information("Thread is exiting...");
        }

    }
}
