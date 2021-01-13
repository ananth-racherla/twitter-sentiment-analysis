using Prometheus.Client.MetricServer;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsumerDemo {
    class Program {
        async static Task Main(string[] args)
        {
            var metricServer = new MetricServer("localhost", 9091, false);
            metricServer.Start();

            var log = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();
            log.Information("Application has started. Ctrl-C to end");

            try {
                var cts = new CancellationTokenSource();

                Console.CancelKeyPress += (sender, eventArgs) => {
                    eventArgs.Cancel = true;  // cancel the cancellation to allow the program to shutdown cleanly
                    cts.Cancel();
                    log.Debug("Token cancelled, exiting the app...");
                };

                var token = cts.Token;
                
                var myTask = Task.Factory.StartNew(stateObject => {
                    var castedToken = (CancellationToken)stateObject;
                    var consumer = new MyConsumer("127.0.0.1:9092", "twitterCG1", "twitter_tweets");
                    consumer.Consume(castedToken);
                }, token, token); 
                /*
                var myTask = Task.Factory.StartNew(stateObject => {
                    var castedToken = (CancellationToken)stateObject;
                    var consumer = new SeekAndAssignConsumer("127.0.0.1:9092", "first_topic", 1, 15);
                    consumer.Consume(castedToken);
                }, token, token);*/


                await myTask;

                log.Information("Application exited.");
            } catch (Exception e) {
                log.Error(e, "App error");
            }
        }
    }
}
