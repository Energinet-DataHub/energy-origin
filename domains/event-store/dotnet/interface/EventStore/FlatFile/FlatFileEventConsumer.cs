using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using EventStore;
using EventStore.Serialization;
using Topics;

namespace EventStore.Flatfile;

public class FlatFileEventConsumer<T> : IDisposable, IEventConsumer<T> where T : EventModel {
    DateTime? fromDate;
    FileSystemWatcher rootWatcher;
    List<FileSystemWatcher> watchers;
    Queue<T> queue = new Queue<T>();

    public FlatFileEventConsumer(string root, string topicPrefix, DateTime? fromDate) {
        this.fromDate = fromDate;

        Console.WriteLine($"Generating directories using: {root}");

        watchers = Directory.GetFiles(root, topicPrefix)
            .Select(path => {
                Console.Write($"Watching directories in: {root}/{path}");
                return createWatcher($"{root}/{path}"); // FIXME: verify root/path
            })
            .ToList();

        rootWatcher = new FileSystemWatcher(root);
        rootWatcher.NotifyFilter = NotifyFilters.LastWrite;
        rootWatcher.Filter = "*.*";
        rootWatcher.Created += OnCreatedDirectory;
        rootWatcher.EnableRaisingEvents = true;
        rootWatcher.IncludeSubdirectories = true;
        rootWatcher.Error += OnError;

        Console.WriteLine(rootWatcher.Path);
    }

    public async Task<T> Consume() {
        while (queue.Count() == 0) {
            await Task.Delay(25);
        }
        return queue.Dequeue();
    }

    private static void OnError(object sender, ErrorEventArgs e) => Console.WriteLine(e.GetException());

    void OnCreatedDirectory(object source, FileSystemEventArgs e) {
        Console.Write($"OnCreatedDirectory: {e.FullPath}");
        watchers.Add(createWatcher(e.FullPath));
    }

    void OnCreatedFile(object source, FileSystemEventArgs e) {
        var payload = File.ReadAllText(e.FullPath);
        var reconstructedEvent = Unpack.Event(payload);
        var reconstructed = Unpack.Message<T>(reconstructedEvent);
        queue.Enqueue(reconstructed);
    }

    public void Dispose() {
        watchers.ForEach(it => {
            it.Created -= OnCreatedFile;
            it.Dispose();
        });
        rootWatcher.Created -= OnCreatedDirectory;
        rootWatcher.Dispose();
    }

    private FileSystemWatcher createWatcher(string path) {
        Console.Write($"Watching files in: {path}");
        var watcher = new FileSystemWatcher();
        watcher.Path = path;
        watcher.NotifyFilter = NotifyFilters.FileName;
        watcher.Filter = "*";
        watcher.Created += new FileSystemEventHandler(OnCreatedFile);
        watcher.EnableRaisingEvents = true;
        return watcher;
    }
}
