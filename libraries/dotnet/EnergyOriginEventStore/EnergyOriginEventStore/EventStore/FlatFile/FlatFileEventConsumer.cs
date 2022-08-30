using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.FlatFile;

internal class FlatFileEventConsumer : IEventConsumer
{
    private readonly IUnpacker unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers;
    private readonly long fromIssued;
    private readonly long fromFraction;
    private readonly FileSystemWatcher rootWatcher;
    private readonly List<FileSystemWatcher> watchers;
    private readonly Action<string, Exception> exceptionHandler;

    public FlatFileEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, string topicPrefix, string? pointer)
    {
        this.unpacker = unpacker;
        this.handlers = handlers;
        this.exceptionHandler = exceptionHandler ?? ((type, exception) => Console.WriteLine($"Type: {type} - Message: {exception.Message}"));

        if (pointer is not null)
        {
            try
            {
                var parts = pointer.Split('-');
                fromIssued = long.Parse(parts[0]);
                fromFraction = long.Parse(parts[1]);
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Pointer '{pointer}' not a valid format");
            }
        }

        watchers = Directory.EnumerateDirectories(FlatFileEventStore.ROOT)
            .Where(it => it.StartsWith($"{FlatFileEventStore.ROOT}/{topicPrefix}") && it.EndsWith(FlatFileEventStore.TOPIC_SUFFIX))
            .Select(it => CreateWatcher(it))
            .ToList();

        rootWatcher = new FileSystemWatcher(FlatFileEventStore.ROOT, "*.topic");
        rootWatcher.Created += OnCreatedDirectory;
        rootWatcher.Error += OnError;
        rootWatcher.EnableRaisingEvents = true;
    }

    private static void OnError(object sender, ErrorEventArgs e) => Console.WriteLine(e.GetException()); // At this point, logging using dependency injection is not implemented, so errors are written to console. As the flat file-based solution is not for production, this is acceptable

    private void OnCreatedDirectory(object source, FileSystemEventArgs e) => watchers.Add(CreateWatcher(e.FullPath));

    private void OnCreatedFile(object source, FileSystemEventArgs e) => Load(e.FullPath);

    private FileSystemWatcher CreateWatcher(string path)
    {
        Directory
            .GetFiles(path)
            .Where(it => it.EndsWith(FlatFileEventStore.EVENT_SUFFIX))
            .ToList()
            .ForEach(Load);

        var watcher = new FileSystemWatcher(path, $"*{FlatFileEventStore.EVENT_SUFFIX}");
        watcher.Created += OnCreatedFile;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void Load(string path)
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

        var type = reconstructed.GetType();
        var typeString = type.ToString();
        var handlers = this.handlers.GetValueOrDefault(type);
        if (handlers == null)
        {
            exceptionHandler.Invoke(typeString, new NotImplementedException($"No handler for event of type {typeString}"));
        }

        (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
        {
            try
            {
                x.Invoke(new Event<EventModel>(reconstructed, EventToPointer(reconstructedEvent)));
            }
            catch (Exception exception)
            {
                exceptionHandler.Invoke(typeString, exception);
            }
        });
    }

    private static string EventToPointer(InternalEvent e) => $"{e.Issued}-{e.IssuedFraction}";

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
}
