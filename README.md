# GoogleCloud_Sample8-PubSub

This code demonstrates how to use the Google Cloud Pub/Sub library to create a topic, create a subscription, publish messages to the topic, and pull messages from the subscription. Let's go through the code step by step:

## 1. Importing necessary libraries:
These lines import the required libraries for working with Google Cloud Pub/Sub and gRPC.
```csharp
using Google.Cloud.PubSub.V1;
using Grpc.Core;
```

## 2. Setting up project, topic, and subscription details:
Here, projectId represents the Google Cloud project ID, topicId represents the ID of the topic to be created, and subscriptionId represents the ID of the subscription to be created.
```csharp
string projectId = "XXXXXXXXXXX";
string topicId = "luis-topic-1";
string subscriptionId = "subscription-first";
```

## 3. Creating a topic:

The code creates a PublisherServiceApiClient instance and attempts to create a topic with the specified topicName. If the topic already exists, it catches the RpcException with the StatusCode.AlreadyExists status and displays a message.
```csharp
PublisherServiceApiClient publisher = PublisherServiceApiClient.Create();
var topicName = TopicName.FromProjectTopic(projectId, topicId);
Topic topic = null;

try
{
    topic = publisher.CreateTopic(topicName);
    Console.WriteLine($"Topic {topic.Name} created.");
}
catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
{
    Console.WriteLine($"Topic {topicName} already exists.");
}
```

## 4. Creating a subscription:
The code creates a SubscriberServiceApiClient instance and attempts to create a subscription with the specified subscriptionName. If the subscription already exists, it catches the RpcException with the StatusCode.AlreadyExists status.
```csharp
SubscriberServiceApiClient subscriber = SubscriberServiceApiClient.Create();
SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
Subscription subscription = null;

try
{
    subscription = subscriber.CreateSubscription(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60);
}
catch (RpcException e) when (e.Status.StatusCode == StatusCode.AlreadyExists)
{
    // Already exists.  That's fine.
}
```

## 5. Publishing messages to the topic:
The code creates a PublisherClient instance and publishes messages to the topic using the PublishAsync method. The messages are stored in the messageTexts list. The Task.WhenAll method is used to wait for all publish tasks to complete.
```csharp
PublisherClient publisher1 = await PublisherClient.CreateAsync(topicName);
// ...
await Task.WhenAll(publishTasks);
```

## 6. Pulling messages from the subscription:
The code calls the PullMessagesSync method to pull messages from the subscription. The pulled messages are printed to the console, and the count of received messages is returned.
```csharp
int messages_Count_received = PullMessagesSync(projectId, subscriptionId, true);
```

(Optional) Call the asynchronous function:
```csharp
await PullMessagesAsync(projectId, subscriptionId, true);
```

## 7. Pulling messages asynchronously (Optional - commented out code):
The code contains commented-out code for pulling messages asynchronously using the PullMessagesAsync method. This method creates a SubscriberClient instance, starts the subscriber, and waits for a specified duration before stopping the subscriber. It returns the count of received messages.

```csharp
async Task<int> PullMessagesAsync(string projectId, string subscriptionId, bool acknowledge)
{
    SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
    SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
    // SubscriberClient runs your message handle function on multiple
    // threads to maximize throughput.
    int messageCount = 0;
    Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
    {
        string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
        Console.WriteLine($"Message {message.MessageId}: {text}");
        Interlocked.Increment(ref messageCount);
        return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
    });
    // Run for 5 seconds.
    await Task.Delay(5000);
    await subscriber.StopAsync(CancellationToken.None);
    // Lets make sure that the start task finished successfully after the call to stop.
    await startTask;
    return messageCount;
}
```
That's a brief overview of what the code does. It sets up a Google Cloud Pub/Sub project, creates a topic and subscription, publishes messages to the topic, and pulls messages from the subscription.



