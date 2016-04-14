using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace processorders
{
    using System.Configuration;
    using Microsoft.WindowsAzure.Storage;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.RetryPolicies;

    class Program
    {
        static void Main(string[] args)
        {
            var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            var options = new QueueRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(2), 10) };
            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference("orders");
            queue.CreateIfNotExists();

            while (true)
            {
                var message = queue.GetMessage(TimeSpan.FromSeconds(60), options);
                if (null == message)
                {
                    Console.WriteLine("No orders found. Hitting the snooze button...");
                    Thread.Sleep(5000);
                    continue;
                }

                using (var messageHeartbeatTimer = KeepMessageHidden(queue, message, options, 45000))
                {
                    try
                    {
                        Console.Write("Processing order {0}...", message.AsString);
                        Thread.Sleep(500);
                        Console.WriteLine("Complete.");
                        queue.DeleteMessage(message, options);
                    }
                    finally
                    {
                        messageHeartbeatTimer?.Stop();
                    }
                }
            }
        }

        private static System.Timers.Timer KeepMessageHidden(CloudQueue queue, CloudQueueMessage message, 
            QueueRequestOptions options, double intervalms)
        {
            var timer = new System.Timers.Timer(intervalms)
            {
                AutoReset = true
            };

            timer.Elapsed += (sender, args) =>
            {
                Console.Write("Still working...");
                queue.UpdateMessage(message, TimeSpan.FromSeconds(30), MessageUpdateFields.Visibility, options);
            };

            timer.Enabled = true;

            return timer;
        }
    }
}
