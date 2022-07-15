using EventStore.Serialization;
using EventStore.Internal;

namespace EventStore.FlatFile;

public class FlatFileEventConsumer : IDisposable, IEventConsumer
{
    private FlatFileEventStore fileStore;
    private IUnpacker unpacker;
    long fromIssued;
    long fromFraction;

    FileSystemWatcher rootWatcher;
    List<FileSystemWatcher> watchers;
    // Queue<EventModel> queue = new Queue<EventModel>();
    Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers;

    public FlatFileEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, FlatFileEventStore fileStore, string topicPrefix, string? pointer)
    {
        this.fileStore = fileStore;
        this.unpacker = unpacker;
        this.handlers = handlers;

        if (pointer is not null)
        {
            try
            {
                var a = pointer.Split('-');
                this.fromIssued = long.Parse(a[0]);
                this.fromFraction = long.Parse(a[1]);
            }
            catch (Exception)
            {
                throw new InvalidDataException("Pointer not a valid format");
            }
        }



        Console.WriteLine($"* Setup watchers using: {fileStore.ROOT} and prefix: {topicPrefix}");

        watchers = Directory.EnumerateDirectories(fileStore.ROOT)
            .Where(it => it.StartsWith($"{fileStore.ROOT}/{topicPrefix}") && it.EndsWith(fileStore.TOPIC_SUFFIX))
            .Select(it => createWatcher(it))
            .ToList();

        rootWatcher = new FileSystemWatcher(fileStore.ROOT, "*.topic");
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
        if (reconstructedEvent.Issued < fromIssued ||
            (reconstructedEvent.Issued == fromIssued && reconstructedEvent.IssuedFraction <= fromFraction))
        {
            return;
        }

        var reconstructed = unpacker.UnpackModel(reconstructedEvent);

        // Optimization Queue events and have worker execute them.
        //queue.Enqueue(reconstructed);

        var t = reconstructed.GetType();
        var b = handlers.GetValueOrDefault(t) ?? throw new NotImplementedException($"No handler for event of type {t.ToString()}");

        b.AsParallel().ForAll(x => x.Invoke(new Event<EventModel>(reconstructed, eventToPointer(reconstructedEvent))));
    }

    string eventToPointer(InternalEvent e)
    {
        return e.Issued.ToString() + "-" + e.IssuedFraction.ToString();
    }

    FileSystemWatcher createWatcher(string path)
    {
        Directory.GetFiles(path)
            .Where(it => it.EndsWith(fileStore.EVENT_SUFFIX)).ToList()
            .ForEach(it => load(it));

        Console.WriteLine($"* Watching files in: {path}");
        var watcher = new FileSystemWatcher(path, $"*{fileStore.EVENT_SUFFIX}");
        watcher.Created += OnCreatedFile;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }
}
