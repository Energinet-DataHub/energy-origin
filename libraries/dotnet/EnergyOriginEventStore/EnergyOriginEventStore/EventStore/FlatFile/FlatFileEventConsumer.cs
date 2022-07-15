using EventStore.Serialization;
using EnergyOriginDateTimeExtension;
using EventStore.Internal;

namespace EventStore.FlatFile;

public class FlatFileEventConsumer : IDisposable, IEventConsumer
{
    private IUnpacker unpacker;
    long fromDate;
    string topicSuffix;
    string eventSuffix;
    FileSystemWatcher rootWatcher;
    List<FileSystemWatcher> watchers;
    // Queue<EventModel> queue = new Queue<EventModel>();
    Dictionary<Type, IEnumerable<Action<EventModel>>> handlers;

    public FlatFileEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<EventModel>>> handlers, string root, string topicSuffix, string eventSuffix, string topicPrefix, DateTime? fromDate)
    {
        this.unpacker = unpacker;
        this.handlers = handlers;
        this.fromDate = fromDate?.ToUnixTime() ?? 0;
        this.topicSuffix = topicSuffix;
        this.eventSuffix = eventSuffix;

        Console.WriteLine($"* Setup watchers using: {root} and prefix: {topicPrefix}");

        watchers = Directory.EnumerateDirectories(root)
            .Where(it => it.StartsWith($"{root}/{topicPrefix}") && it.EndsWith(topicSuffix))
            .Select(it => createWatcher(it))
            .ToList();

        rootWatcher = new FileSystemWatcher(root, "*.topic");
        rootWatcher.Created += OnCreatedDirectory;
        rootWatcher.Error += OnError;
        rootWatcher.EnableRaisingEvents = true;
    }

    // public async Task<EventModel> Consume()
    // {
    //     while (queue.Count() == 0)
    //     {
    //         await Task.Delay(25);
    //     }
    //     return queue.Dequeue();
    // }

    private static void OnError(object sender, ErrorEventArgs e) => Console.WriteLine(e.GetException());

    void OnCreatedDirectory(object source, FileSystemEventArgs e) => watchers.Add(createWatcher(e.FullPath));

    void OnCreatedFile(object source, FileSystemEventArgs e)
    {
        load(e.FullPath);
    }

    public void Dispose()
    {
        watchers.ForEach(it =>
        {
            it.Created -= OnCreatedFile;
            it.Error -= OnError;
            it.Dispose();
        });
        rootWatcher.Created -= OnCreatedDirectory;
        rootWatcher.Error -= OnError;
        rootWatcher.Dispose();
    }

    void load(string path)
    {
        var payload = File.ReadAllText(path);
        var reconstructedEvent = unpacker.UnpackEvent(payload);
        if (reconstructedEvent.Issued < fromDate)
        {
            return;
        }
        var reconstructed = unpacker.UnpackModel(reconstructedEvent);

        // Optimization Queue events and have worker execute them.
        //queue.Enqueue(reconstructed);

        var t = reconstructed.GetType();
        var b = handlers.GetValueOrDefault(t) ?? throw new NotImplementedException($"No handler for event of type {t.ToString()}");

        b.AsParallel().ForAll(x => x.Invoke(reconstructed));
    }

    FileSystemWatcher createWatcher(string path)
    {
        Directory.GetFiles(path)
            .Where(it => it.EndsWith(eventSuffix)).ToList()
            .ForEach(it => load(it));

        Console.WriteLine($"* Watching files in: {path}");
        var watcher = new FileSystemWatcher(path, $"*{eventSuffix}");
        watcher.Created += OnCreatedFile;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }
}
