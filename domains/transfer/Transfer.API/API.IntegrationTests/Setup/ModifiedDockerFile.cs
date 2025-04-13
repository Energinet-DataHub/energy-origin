using System;
using System.IO;

namespace API.IntegrationTests.Setup;

public class ModifiedDockerfile : IDisposable
{
    private readonly string _tempFile;

    public string FullPath => this._tempFile;

    public string FileName => Path.GetFileName(this._tempFile);

    public ModifiedDockerfile(string sourcePath, Func<string, string> modification)
    {
        this._tempFile = sourcePath + ".tmp";
        string str = File.ReadAllText(sourcePath);
        File.WriteAllText(this._tempFile, modification(str));
    }

    public void Dispose()
    {
        if (!File.Exists(this._tempFile))
            return;
        File.Delete(this._tempFile);
    }

    ~ModifiedDockerfile()
    {
        if (!File.Exists(this._tempFile))
            return;
        File.Delete(this._tempFile);
    }
}
