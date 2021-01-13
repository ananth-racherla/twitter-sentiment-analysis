using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace ProducerDemo {
    class Program {
        static void Main(string[] args)
        {
            var config = new Dictionary<string, object>(){
                { "bootstrap.servers", "127.0.0.1:9092" }
            };

            using (var producer = new Producer<string, string>(config, new StringSerializer(Encoding.UTF8), new StringSerializer(Encoding.UTF8))) {
                var deliveryHandler = new DeliveryHandler();
                
                for (int i = 0; i < 10 ; i++) {
                    producer.ProduceAsync("first_topic" /*topic*/, $"id_{i}" /*key*/, $"Merry Christmas {i}" /*message*/, deliveryHandler);
                }
                producer.Flush(5000);
            }

            Console.ReadLine();

        }
    }

    class DeliveryHandler : IDeliveryHandler<string, string> {
        public bool MarshalData => throw new NotImplementedException();

        public void HandleDeliveryReport(Message<string, string> message)
        {
            Console.WriteLine($"Producer: metadata \n Topic: {message.Topic}\n Partition: {message.Partition}\n Offset: {message.Offset}\n TimeStamp: {message.Timestamp.UtcDateTime.ToLongTimeString()}");
        }
    }
}
