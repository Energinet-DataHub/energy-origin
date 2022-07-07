using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using EventStore;
using EventStore.Serialization;
using Topics;
using EnergyOriginDateTimeExtension;

namespace EventStore.Flatfile;

public class FlatFileEventConsumer<T> : IDisposable, IEventConsumer<T> where T : EventModel {
    long fromDate;
    string topicSuffix;
    string eventSuffix;
    FileSystemWatcher rootWatcher;
    List<FileSystemWatcher> watchers;
    Queue<T> queue = new Queue<T>();

    public FlatFileEventConsumer(string root, string topicSuffix, string eventSuffix, string topicPrefix, DateTime? fromDate) {
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

    public async Task<T> Consume() {
        while (queue.Count() == 0) {
            await Task.Delay(25);
        }
        return queue.Dequeue();
    }

    private static void OnError(object sender, ErrorEventArgs e) => Console.WriteLine(e.GetException());

    void OnCreatedDirectory(object source, FileSystemEventArgs e) => watchers.Add(createWatcher(e.FullPath));

    void OnCreatedFile(object source, FileSystemEventArgs e) {
        load(e.FullPath);
    }

    public void Dispose() {
        watchers.ForEach(it => {
            it.Created -= OnCreatedFile;
            it.Error -= OnError;
            it.Dispose();
        });
        rootWatcher.Created -= OnCreatedDirectory;
        rootWatcher.Error -= OnError;
        rootWatcher.Dispose();
    }

    void load(string path) {
        var payload = File.ReadAllText(path);
        var reconstructedEvent = Unpack.Event(payload);
        if (reconstructedEvent.Issued < fromDate) {
            return;
        }
        var reconstructed = Unpack.Message<T>(reconstructedEvent);
        queue.Enqueue(reconstructed);
    }

    FileSystemWatcher createWatcher(string path) {
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
