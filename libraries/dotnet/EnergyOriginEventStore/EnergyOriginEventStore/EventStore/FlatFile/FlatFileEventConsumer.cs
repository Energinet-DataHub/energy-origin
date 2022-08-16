using EnergyOriginEventStore.EventStore.Internal;
using EnergyOriginEventStore.EventStore.Serialization;

namespace EnergyOriginEventStore.EventStore.FlatFile;

internal class FlatFileEventConsumer : IEventConsumer
{
    private readonly FlatFileEventStore _fileStore;
    private readonly IUnpacker _unpacker;
    private readonly Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> _handlers;
    private readonly long _fromIssued;
    private readonly long _fromFraction;
    private readonly FileSystemWatcher _rootWatcher;
    private readonly List<FileSystemWatcher> _watchers;
    private Action<string, Exception> _exceptionHandler;

    public FlatFileEventConsumer(IUnpacker unpacker, Dictionary<Type, IEnumerable<Action<Event<EventModel>>>> handlers, Action<string, Exception>? exceptionHandler, FlatFileEventStore fileStore, string topicPrefix, string? pointer)
    {
        _fileStore = fileStore;
        _unpacker = unpacker;
        _handlers = handlers;
        _exceptionHandler = exceptionHandler ?? ((type, exception) => Console.WriteLine($"Type: {type} - Message: {exception.Message}"));

        if (pointer is not null)
        {
            try
            {
                var parts = pointer.Split('-');
                _fromIssued = long.Parse(parts[0]);
                _fromFraction = long.Parse(parts[1]);
            }
            catch (Exception)
            {
                throw new InvalidDataException($"Pointer '{pointer}' not a valid format");
            }
        }

        _watchers = Directory.EnumerateDirectories(fileStore.ROOT)
            .Where(it => it.StartsWith($"{fileStore.ROOT}/{topicPrefix}") && it.EndsWith(fileStore.TOPIC_SUFFIX))
            .Select(it => CreateWatcher(it))
            .ToList();

        _rootWatcher = new FileSystemWatcher(fileStore.ROOT, "*.topic");
        _rootWatcher.Created += OnCreatedDirectory;
        _rootWatcher.Error += OnError;
        _rootWatcher.EnableRaisingEvents = true;
    }

    private static void OnError(object sender, ErrorEventArgs e) => Console.WriteLine(e.GetException()); // At this point, logging using dependency injection is not implemented, so errors are written to console. As the flat file-based solution is not for production, this is acceptable

    private void OnCreatedDirectory(object source, FileSystemEventArgs e) => _watchers.Add(CreateWatcher(e.FullPath));

    private void OnCreatedFile(object source, FileSystemEventArgs e)
    {
        Load(e.FullPath);
    }

    private FileSystemWatcher CreateWatcher(string path)
    {
        Directory
            .GetFiles(path)
            .Where(it => it.EndsWith(_fileStore.EVENT_SUFFIX))
            .ToList()
            .ForEach(Load);

        var watcher = new FileSystemWatcher(path, $"*{_fileStore.EVENT_SUFFIX}");
        watcher.Created += OnCreatedFile;
        watcher.Error += OnError;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    private void Load(string path)
    {
        var payload = File.ReadAllText(path);
        var reconstructedEvent = _unpacker.UnpackEvent(payload);
        if (reconstructedEvent.Issued < _fromIssued ||
            (reconstructedEvent.Issued == _fromIssued && reconstructedEvent.IssuedFraction <= _fromFraction))
        {
            return;
        }

        var reconstructed = _unpacker.UnpackModel(reconstructedEvent);

        // Optimization Queue events and have worker execute them.
        //queue.Enqueue(reconstructed);

        var type = reconstructed.GetType();
        var typeString = type.ToString();
        var handlers = _handlers.GetValueOrDefault(type);
        if (handlers == null)
        {
            _exceptionHandler.Invoke(typeString, new NotImplementedException($"No handler for event of type {type.ToString()}"));
        }

        (handlers ?? Enumerable.Empty<Action<Event<EventModel>>>()).AsParallel().ForAll(x =>
        {
            try
            {
                x.Invoke(new Event<EventModel>(reconstructed, EventToPointer(reconstructedEvent)));
            }
            catch (Exception exception)
            {
                _exceptionHandler.Invoke(typeString, exception);
            }
        });
    }

    private static string EventToPointer(InternalEvent e) => $"{e.Issued}-{e.IssuedFraction}";

    public void Dispose()
    {
        _watchers.ForEach(it =>
        {
            it.Created -= OnCreatedFile;
            it.Error -= OnError;
            it.Dispose();
        });
        _rootWatcher.Created -= OnCreatedDirectory;
        _rootWatcher.Error -= OnError;
        _rootWatcher.Dispose();
    }
}
