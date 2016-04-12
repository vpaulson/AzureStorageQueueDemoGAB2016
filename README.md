# Azure Storage Queue Demo - GAB2016 #

## Introduction ##

In this demo we will be creating a couple simple programs to demonstrate using Azure Storage Queues for message passing. We will be simulating an ordering system were orders are placed in a queue (perhaps from a website) and then a background application monitors this queue and "processes" those orders.

## Tools Used ##

Below are the tools I used in this Demo. Similar versions of Visual Studio and the Azure SDK should suffice, though there may be some limitations on the versions of Visual Studio you can use with the latest versions of the Azure SDK.

- Visual Studio 2015 Community Edition
- Azure SDK 2.8
- Azure Free Trial Subscription

You can also use the Azure Storage Emulator that is installed with the Azure SDK by using **UseDevelopmentStorage=true** in place of the Storage Account connection string rather than an actual Azure Subscription.

## Part 1 - Connecting to a Storage Account

In this section we will be setting up a new project to connect to Azure Storage.

1. First you will need to create a new Windows Console Application Project in Visual Studio named **placeorders** and add it to a new Solution named **AzureStorageQueuesDemo**.
2. Use NuGet to add the **WindowsAzure.Storage** package to your new project. It will install several dependencies at the same time.
3. Retrieve your Storage Account Access Key from the Azure Portal. You can copy the entire connection string from the Access Keys popup. Add this connection string to your App.config as an appsetting. Alternatively you can use the value of **UseDevelopmentStorage=true** instead to use the Storage Emulator.
4. Add a references to **System.Configuration** and **Microsoft.WindowsAzure.Storage** to your project.

	    using System.Configuration;
	    using Microsoft.WindowsAzure.Storage;

5. Now we are going to get a reference to our Cloud Storage Account:

		var account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

## Part 2 - Creating a queue and adding a message

1. Now we are going to create a queue client object, get a reference to our **orders** queue, and then create it if it doesn't exist:

	    var queueClient = account.CreateCloudQueueClient();
	    var queue = queueClient.GetQueueReference("orders");
	    queue.CreateIfNotExists();

2. Then we are going to add a message with the **queue.AddMessage** function:

		queue.AddMessage(new CloudQueueMessage("ThisIsMyMessage"));

3. Optionally you can add a **Console.ReadKey()** to the end of your **Main()** just to prevent the console window from closing until you press a key after it has completed executing.
4. If you run this application it should create a new queue and then add your message to that queue. You can then use a tool such as the Cloud Explorer built into VS2015 or [Azure Storage Explorer](ttps://azurestorageexplorer.codeplex.com/ "https://azurestorageexplorer.codeplex.com/") to see your message.