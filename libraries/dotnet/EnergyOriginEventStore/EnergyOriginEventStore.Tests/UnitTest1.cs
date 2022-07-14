using EventStore;
using EventStore.Serialization;
using Newtonsoft.Json;
using Topics;
using Xunit;

namespace EnergyOriginEventStore.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        Task.Run(async () =>
        {
            // How event can be de/serialized

            Console.WriteLine("# Example of en-/de-coding event messages:\n");

            var message = new Said("Anton Actor", "I like to act!");

            Console.WriteLine($"- Sending message of type '{message.Type}':\n{message.Data}");

            var package = Event.From(message);
            var payload = JsonConvert.SerializeObject(package);

            Console.WriteLine($"\n- Packaged:\n{payload}");

            var reconstructedEvent = Unpack.Event(payload);

            var reconstructed = Unpack.Message<Said>(reconstructedEvent);

            Console.WriteLine($"\n- Received message of type '{reconstructed.Type}':\n{reconstructed.Data}");

            Console.WriteLine("\n");



            // Usage of event store

            Console.WriteLine("# Example of using an event store:\n");

            var eventStore = EventStoreFactory<Said>.create();

            Console.WriteLine($"\n- Produce message.");
            eventStore.Produce(message, new List<String> { "Gossip", "Tabloid" });

            Console.WriteLine($"\n- Make consumer.");
            var consumer = eventStore.MakeConsumer("Gossip");

            while (true) {
                Console.WriteLine($"\n- Consume:");
                var saidEvent = await consumer.Consume();
                Console.WriteLine($"I heard that {saidEvent.Actor} said {saidEvent.Statement}");
                break;
            }

            // House keeping

            Directory.Delete("store", true);

        }).Wait();
    }
}