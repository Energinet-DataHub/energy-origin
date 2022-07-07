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

        watchers = Directory.GetFiles(root, topicPrefix)
            .Select(path => createWatcher($"{root}/{path}")) // FIXME: verify root/path
            .ToList();

        rootWatcher = new FileSystemWatcher();
        rootWatcher.Path = root;
        rootWatcher.NotifyFilter = NotifyFilters.DirectoryName;
        rootWatcher.Filter = "*";
        rootWatcher.Created += new FileSystemEventHandler(OnCreatedDirectory);
        rootWatcher.EnableRaisingEvents = true;
    }

    public async Task<T> Consume() {
        while (queue.Count() == 0) {
            await Task.Delay(25);
        }
        return queue.Dequeue();
    }

    void OnCreatedDirectory(object source, FileSystemEventArgs e) {
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
        var watcher = new FileSystemWatcher();
        watcher.Path = path;
        watcher.NotifyFilter = NotifyFilters.FileName;
        watcher.Filter = "*";
        watcher.Created += new FileSystemEventHandler(OnCreatedFile);
        watcher.EnableRaisingEvents = true;
        return watcher;
    }
}
