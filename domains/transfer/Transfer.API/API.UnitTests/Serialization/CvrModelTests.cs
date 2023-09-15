using API.Cvr.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace API.UnitTests.Serialization;
public class CvrModelTests
{
    [Theory]
    [InlineData("cvr_response.json")]
    public void DeserializeToModel_ExpectSuccess(string filePath)
    {
        var jsonString = LoadFileToString(filePath);

        var obj = JsonConvert.DeserializeObject<Root>(jsonString);

        obj.Should().NotBeNull();
    }

    private static string LoadFileToString(string filePath)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var path = Path.IsPathRooted(filePath)
            ? filePath
            : Path.GetRelativePath(currentDir, filePath);

        if (!File.Exists(path))
        {
            throw new ArgumentException($"Could not find file at path: {path}. Directory: {currentDir}");
        }
        return File.ReadAllText(filePath);
    }
}
