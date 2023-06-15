using Google.Cloud.PubSub.V1;
using Grpc.Core;

string projectId = "XXXXXXXXXXXXX";
string topicId = "luis-topic-1";
string subscriptionId = "subscription-first";

//Create a topic
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

List<string> messageTexts = new List<string>();
messageTexts.Add("First message");
messageTexts.Add("Second message");
messageTexts.Add("Third message");
messageTexts.Add("Fourth message");
messageTexts.Add("Fifth message");


//Create a Subscriptio for the topic
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


//Publish messages to the above created topic
PublisherClient publisher1 = await PublisherClient.CreateAsync(topicName);

int publishedMessageCount = 0;
var publishTasks = messageTexts.Select(async text =>
{
    try
    {
        string message = await publisher1.PublishAsync(text);
        Console.WriteLine($"Published message {message}");
        Interlocked.Increment(ref publishedMessageCount);
    }
    catch (Exception exception)
    {
        Console.WriteLine($"An error ocurred when publishing message {text}: {exception.Message}");
    }
});
await Task.WhenAll(publishTasks);


//Pull the messages from the Topic to the Subscription
//await PullMessagesAsync(projectId, subscriptionId, true);

int messages_Count_received = PullMessagesSync(projectId, subscriptionId, true);

//async Task<int> PullMessagesAsync(string projectId, string subscriptionId, bool acknowledge)
//{
//    SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
//    SubscriberClient subscriber = await SubscriberClient.CreateAsync(subscriptionName);
//    // SubscriberClient runs your message handle function on multiple
//    // threads to maximize throughput.
//    int messageCount = 0;
//    Task startTask = subscriber.StartAsync((PubsubMessage message, CancellationToken cancel) =>
//    {
//        string text = System.Text.Encoding.UTF8.GetString(message.Data.ToArray());
//        Console.WriteLine($"Message {message.MessageId}: {text}");
//        Interlocked.Increment(ref messageCount);
//        return Task.FromResult(acknowledge ? SubscriberClient.Reply.Ack : SubscriberClient.Reply.Nack);
//    });
//    // Run for 5 seconds.
//    await Task.Delay(5000);
//    await subscriber.StopAsync(CancellationToken.None);
//    // Lets make sure that the start task finished successfully after the call to stop.
//    await startTask;
//    return messageCount;
//}

int PullMessagesSync(string projectId, string subscriptionId, bool acknowledge)
{
    SubscriptionName subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);
    SubscriberServiceApiClient subscriberClient = SubscriberServiceApiClient.Create();
    int messageCount = 0;
    try
    {
        // Pull messages from server,
        // allowing an immediate response if there are no messages.
        PullResponse response = subscriberClient.Pull(subscriptionName, maxMessages: 20);
        // Print out each received message.
        foreach (ReceivedMessage msg in response.ReceivedMessages)
        {
            string text = System.Text.Encoding.UTF8.GetString(msg.Message.Data.ToArray());
            Console.WriteLine($"Message {msg.Message.MessageId}: {text}");
            Interlocked.Increment(ref messageCount);
        }
        // If acknowledgement required, send to server.
        if (acknowledge && messageCount > 0)
        {
            subscriberClient.Acknowledge(subscriptionName, response.ReceivedMessages.Select(msg => msg.AckId));
        }
    }
    catch (RpcException ex) when (ex.Status.StatusCode == StatusCode.Unavailable)
    {
        // UNAVAILABLE due to too many concurrent pull requests pending for the given subscription.
    }
    return messageCount;
}
