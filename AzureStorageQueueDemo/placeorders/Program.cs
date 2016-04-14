using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace placeorders
{
    using System.Configuration;
    using Microsoft.WindowsAzure.Storage;
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

            for (int orderid = 1000; orderid < 1100; orderid++)
            {
                queue.AddMessage(new CloudQueueMessage(orderid.ToString()), null, null, options);
                Console.WriteLine("Order {0} placed", orderid);
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
