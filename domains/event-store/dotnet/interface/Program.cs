using System;
using EventStore;

// FIXME: written while interface wont compile

var eventStore = EventStoreFactory.create();

eventStore.Produce(new Said("Anton Actor", "I like to act!"), new List<String> { "Gossip", "Tabloid" });

var consumer = eventStore.MakeConsumer<Said>("Gossip");

while (true) {
    var saidEvent = consumer.Consume();
    Console.WriteLine($"I heard that {saidEvent.Actor} said {saidEvent.Statement}");
}