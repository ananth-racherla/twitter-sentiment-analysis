using Serilog;
using Serilog.Core;
using System;
using Tweetinvi;

namespace TwitterProducer {
    class Program {
        static Logger log = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();

        static void Main(string[] args)
        {
            CreateTwitterClient();
        }


        static void CreateTwitterClient()
        {

            Auth.SetUserCredentials(""); //TODO Add Auth Creds here
            var stream = Stream.CreateFilteredStream();
            stream.AddTrack("trump");

            var producer = new MyProducer("twitter_tweets", "127.0.0.1:9092");

            stream.MatchingTweetReceived += (sender, args) =>
            {
                log.Information(args.Tweet.Text);
                foreach (var track in args.MatchingTracks) {
                    producer.Produce(track, args.Tweet.Text);
                }
            };

            Console.CancelKeyPress += (sender, eventArgs) => {
                eventArgs.Cancel = true;  // cancel the cancellation to allow the program to shutdown cleanly
                stream.StopStream();
                log.Debug("Exiting the app...");
            };

            stream.StreamStopped += (sender, args) =>
            {
                stream.StartStreamMatchingAllConditions();
            };

            stream.StartStreamMatchingAllConditions();
        }
    }
}
