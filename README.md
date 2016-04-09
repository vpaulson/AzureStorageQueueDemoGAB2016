# Azure Storage Queue Demo - GAB2016 #

## Introduction ##

In this demo we will be creating a couple simple programs to demonstrate using Azure Storage Queues for message passing. We will be simulating an ordering system were orders are placed in a queue (perhaps from a website) and then a background application monitors this queue and "processes" those orders.

## Tools Used ##

Below are the tools I used in this Demo. Similar versions of Visual Studio and the Azure SDK should suffice, though there may be some limitations on the versions of Visual Studio you can use with the latest versions of the Azure SDK.

- Visual Studio 2015 Community Edition
- Azure SDK 2.8
- Azure Free Trial Subscription

You can also use the Azure Storage Emulator that is installed with the Azure SDK by using "UseDevelopmentStorage=true" in place of the Storage Account connection string rather than an actual Azure Subscription.

## Part 1 - Connecting to a Storage Account

In this section we will be setting up a new project to connect to Azure Storage.

1. First you will need to create a new Visual Studio Windows Console Application Project named **placeorders** and add it to a new Solution named **AzureStorageQueuesDemoGab2016** or something similar.
2. Use NuGet to add the **WindowsAzure.Storage** package to your new project. It will install several dependencies at the same time.