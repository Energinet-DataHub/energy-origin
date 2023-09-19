using API.Cvr.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Xunit.Sdk;

namespace API.UnitTests.Serialization;

public class CvrModelTests
{
    [Theory]
    [InlineData("Serialization/cvr_response.json")]
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

public sealed class EmbeddedResourceDataAttribute : DataAttribute
{
    private readonly string[] _args;

    public EmbeddedResourceDataAttribute(params string[] args)
    {
        _args = args;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var result = new object[_args.Length];
        for (var index = 0; index < _args.Length; index++)
        {
            result[index] = ReadManifestData(_args[index]);
        }
        return new[] { result };
    }

    public static string ReadManifestData(string resourceName)
    {
        var assembly = typeof(EmbeddedResourceDataAttribute).GetTypeInfo().Assembly;
        //resourceName = resourceName.Replace("/", ".");
        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new InvalidOperationException("Could not load manifest resource stream.");
            }
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

