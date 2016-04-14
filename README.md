# Azure Storage Queue Demo - GAB2016 #

## Introduction ##

In this demo we will be creating a couple simple programs to demonstrate using Azure Storage Queues for message passing. We will be simulating an ordering system were orders are placed in a queue (perhaps from a website) and then a background application monitors this queue and "processes" those orders.

## Tools Used ##

Below are the tools I used in this Demo. Similar versions of Visual Studio and the Azure SDK should suffice, though there may be some limitations on the versions of Visual Studio you can use with the latest versions of the Azure SDK.

- Visual Studio 2015 Community Edition
- Azure SDK 2.8
- Azure Free Trial Subscription

You can also use the Azure Storage Emulator that is installed with the Azure SDK by using **UseDevelopmentStorage=true** in place of the Storage Account connection string rather than an actual Azure Subscription.

## Part 1 - Connecting to a Storage Account ##

In this section we will be setting up a new project to connect to Azure Storage.

1. First you will need to create a new Windows Console Application Project in Visual Studio named **placeorders** and add it to a new Solution named **AzureStorageQueuesDemo**.
2. Use NuGet to add the **WindowsAzure.Storage** package to your new project. It will install several dependencies at the same time.
3. Retrieve your Storage Account Access Key from the Azure Portal. You can copy the entire connection string from the Access Keys popup. Add this connection string to your App.config as an appsetting. Alternatively you can use the value of **UseDevelopmentStorage=true** instead to use the Storage Emulator.
4. Add a references to **System.Configuration** and **Microsoft.WindowsAzure.Storage** to your project.

	    using System.Configuration;
	    using Microsoft.WindowsAzure.Storage;

5. Now we are going to get a reference to our Cloud Storage Account:

		var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

## Part 2 - Creating a queue and adding messages ##

1. Now we are going to create a queue client object, get a reference to our **orders** queue, and then create it if it doesn't exist:

	    var queueClient = account.CreateCloudQueueClient();
	    var queue = queueClient.GetQueueReference("orders");
	    queue.CreateIfNotExists();

2. Add a series of fake orders to our queue the **queue.AddMessage** function:

        for (int orderid = 1000; orderid < 1100; orderid++)
        {
            queue.AddMessage(new CloudQueueMessage(orderid.ToString()));
            Console.WriteLine("Order {0} placed", orderid);
        }

3. Optionally you can add a **Console.ReadKey()** to the end of your **Main()** just to prevent the console window from closing until you press a key after it has completed executing:

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();

If you run this application it should create a new queue and then add your messages to that queue. You can then use a tool such as the Cloud Explorer built into VS2015 or [Azure Storage Explorer](ttps://azurestorageexplorer.codeplex.com/ "https://azurestorageexplorer.codeplex.com/") to see your messages.

## Part 3 - Getting and deleting messages from a queue ##

1. Now create another Windows Console Application project in the same solution named **processorders**.
2. Repeat Part 1 steps 2 through 5 and Part 2 step 1 with this new project to connect to your queue.
3. We want the **processorders** application to continue to run so lets put the "processing" code in a **while(true)** loop and get a message:

	    while (true)
	    {
	        var message = queue.GetMessage();

4. If no message is retrieved (empty queue) then lets wait a bit and check again:

	        if (null == message)
	        {
	            Console.WriteLine("No orders found. Hitting the snooze button...");
	            Thread.Sleep(5000);
	            continue;
	        }

5. However, if we did manage to retrieve a message, lets "process" it and then delete the message:

            Console.Write("Processing order {0}...", message.AsString);
            Thread.Sleep(500);
            Console.WriteLine("Complete.");
            queue.DeleteMessage(message);
		}

If we now execute **processorders** we should see it work its way through the orders we had previously placed in the queue with the **placeorders** application.

## Part 4 - Handling Visibility Timeout ##

Since Azure Storage Queues operate on a At-Least-Once delivery method, we need to update our code to be aware of this visibility timeout to prevent unexpected handling of our messages.

1. Modify the **GetMessages** line in the **processorders** project to include a visibility timeout:

		var message = queue.GetMessage(TimeSpan.FromSeconds(60));

2. Now we are going to create a new function **KeepMessageHidden** that will create and start a **System.Timers.Timer**. First create a new timer and set **AutoReset** to **true** to allow the timer to restart after each time it is triggered:

		private static System.Timers.Timer KeepMessageHidden(CloudQueue queue, CloudQueueMessage message, double intervalms)
        {
            var timer = new System.Timers.Timer(intervalms)
            {
                AutoReset = true
            };

3. Now attach a new event to the **Elapsed** event handler which will reset the visibility timeout of the message using the **UpdateMessage** method of the queue.

			timer.Elapsed += (sender, args) =>
            {
                Console.Write("Still working...");
                queue.UpdateMessage(message, TimeSpan.FromSeconds(60), MessageUpdateFields.Visibility);
            };

4. Then enable the timer and return it:

            timer.Enabled = true;

            return timer;
        }

5. We will now modify our original "processing" step to execute this timer to ensure the message doesn't become visible again while it is still being worked on. First we will get an instance of the update timer from the **KeepMessagesHidden** method we created in the last steps and make sure to set the timer interval to something less than the visibility timeout:

		using(var messageHeartbeatTimer = KeepMessageHidden(queue, message, 45000))
		{

6. Wrap the processing step and the message delete call in a **try...finally**:

			try
	        {
	            Console.Write("Processing order {0}...", message.AsString);
	            Thread.Sleep(500);
	            Console.WriteLine("Complete.");
	            queue.DeleteMessage(message, options);
	        }
	        finally
	        {

7. In the **finally** block we are going to stop the timer:

				
                messageHeartbeatTimer?.Stop();
            }
        }

If you would like to see our Timer in action, try adjusting the visibility timeout of the messages to something small like 5 seconds and then set the **Thread.Sleep** of the "processing" step to something longer like 11 seconds. You should then see the "Still Working..." message a couple times on each message.

## Part 5 - Retry Options ##

Unfortunately, we do not live in a perfect world and as a result there can occasionally be errors accessing the queue. When those errors are potentially transient server issues we should retry our queue operations rather than fail.

1. We are going to first modify the **placeorders** project to use the **RetryPolicy** property of a **QueueRequestOptions** object:

		var options = new QueueRequestOptions { RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(2), 10) };

	There are other **RetryPolicy's** that could be used such as **ExpotentialRetry** but for the example **LinearRetry** is sufficient. We are using a retry delay of 2 seconds with a max of 10 retry attempts.

2. You will need to add the following using statements as well:

	    using Microsoft.WindowsAzure.Storage.Queue;
	    using Microsoft.WindowsAzure.Storage.RetryPolicies;

3. Now modify the **AddMessage** call to pass the **QueueRequestOptions** object we just created:

        queue.AddMessage(new CloudQueueMessage(orderid.ToString()), null, null, options);

4. Next we will modify the **processorders** project to use the same **LinearRetry** policy. Repeat steps 1 and 2 for this project.
5. Modify the **GetMessage** call to pass the **QueueRequestOptions**:

		var message = queue.GetMessage(TimeSpan.FromSeconds(60), options);

6. Update the **DeleteMessage** call:

		queue.DeleteMessage(message, options);

7. Update our **KeepMessageHidden** method to take a **QueueRequestOptions** object:

        private static System.Timers.Timer KeepMessageHidden(CloudQueue queue, CloudQueueMessage message, 
            QueueRequestOptions options, double intervalms)

8. Update our call to the **KeepMessageHidden**:

		using (var messageHeartbeatTimer = KeepMessageHidden(queue, message, options, 45000))

9. Finally modify the **UpdateMessage** call within the **KeepMessageHidden** method:

		queue.UpdateMessage(message, TimeSpan.FromSeconds(30), MessageUpdateFields.Visibility, options);

Now our applications will retry on transient server errors such as most of the 500 HTTP errors.

## Part 6 - Other Fun ##

At this point our demo is essentially done. However if you would like to experiment a little more I recommend you simulate a situation where you have multiple instances of the **processorders** executable running but the visibility timeout is shorter than the amount of time it takes to "process" the order. If you output the message to the console or debug so you can inspect the properties of the message you should be able to see a message retrieved by executable A, then get retrieved again by executable B after the visibility timeout expires, and then executable A get a 404 error attempting to delete the message because the **PopReceipt** has changed when executable B retrieved the message.