using Confluent.Kafka;
using Confluent.Kafka.Serialization;
using Prometheus.Client;
using Serilog;
using Serilog.Core;
using SimpleNetNlp;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ConsumerDemo {
    public class MyConsumer{
        Logger log = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();
        public string BootstapServer { get; }
        public string GroupId { get; }
        public string Topic { get; }
        Histogram hist = Metrics.CreateHistogram("twitterSentiment", "Twitter Sentiment", new double[] { 0, 1, 2, 3, 4 }, new[] { "track" });

        public MyConsumer(string bootstapServer, string groupId, string topic)
        {
            BootstapServer = bootstapServer;
            GroupId = groupId;
            Topic = topic;
        }

        private void OnMessage(object sender, Message<string, string> message)
        {
            //log.Debug($"Received new metadata \n Topic: {message.Topic}\n Partition: {message.Partition}\n Offset: {message.Offset}\n TimeStamp: {message.Timestamp.UtcDateTime.ToLongTimeString()}\n Key: { message.Key}\n Value: {message.Value} ");

            try {
                if (string.IsNullOrEmpty(message.Value))
                    return;

                var sanitized = sanitize(message.Value);
                if (string.IsNullOrEmpty(sanitized))
                    return;

                var sentence = new Sentence(sanitized);

                var sentiment = sentence.Sentiment;
                log.Debug($"{sentiment}|({(int)sentiment})|{message.Key}|{message.Value}");

                hist.Labels(message.Key).Observe(4-(double)sentiment);
            } catch {
                //Ignore all exceptions
            }
        }

        private static string sanitize(string raw)
        {
            return Regex.Replace(raw, @"(@[A-Za-z0-9]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)", " ").ToString();
        }

        public void Consume(CancellationToken token)
        {
            // the group.id property must be specified when creating a consumer, even 
            // if you do not intend to use any consumer group functionality.

            var config = new Dictionary<string, object>()  {
                { "bootstrap.servers", BootstapServer },
                { "group.id", GroupId }
            };

            using (var consumer = new Consumer<string, string>(config, new StringDeserializer(Encoding.UTF8), new StringDeserializer(Encoding.UTF8))) {
                consumer.Subscribe(new List<string> { Topic });


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
