PureNSQSharp
========

A .NET Standard client library for [NSQ](https://github.com/nsqio/nsq), a realtime distributed messaging platform.

## Quick Install

PureNSQSharp is a client library that talks to the `nsqd` (message queue) and `nsqlookupd` (topic discovery service).

```
nsqlookupd

nsqd -lookupd-tcp-address=127.0.0.1:4160
```


#### Simple Producer

```cs
using System;
using PureNSQSharp;

class Program
{
    static void Main()  
    {
        var producer = new Producer("127.0.0.1:4150");
        producer.Publish("test-topic-name", "Hello!");

        Console.WriteLine("Enter your message (blank line to quit):");
        string line = Console.ReadLine();
        while (!string.IsNullOrEmpty(line))
        {
            producer.Publish("test-topic-name", line);
            line = Console.ReadLine();
        }

        producer.Stop();
    }
}
```

#### Simple Consumer

```cs
using System;
using System.Text;
using PureNSQSharp;

class Program
{
    static void Main()  
    {
        // Create a new Consumer for each topic/channel
        var consumer = new Consumer("test-topic-name", "channel-name");
        consumer.AddHandler(new MessageHandler());
        consumer.ConnectToNsqLookupd("127.0.0.1:4161");

        Console.WriteLine("Listening for messages. If this is the first execution, it " +
                          "could take up to 60s for topic producers to be discovered.");
        Console.WriteLine("Press enter to stop...");
        Console.ReadLine();

        consumer.Stop();
    }
}

public class MessageHandler : IHandler
{
    /// <summary>Handles a message.</summary>
    public void HandleMessage(IMessage message)
    {
        string msg = Encoding.UTF8.GetString(message.Body);
        Console.WriteLine(msg);
    }

    /// <summary>
    /// Called when a message has exceeded the specified <see cref="Config.MaxAttempts"/>.
    /// </summary>
    /// <param name="message">The failed message.</param>
    public void LogFailedMessage(IMessage message)
    {
        // Log failed messages
    }
}
```

## Pull Requests

Pull requests and issues are very welcome and appreciated.

## License

This project is open source and released under the [MIT license.](LICENSE)
