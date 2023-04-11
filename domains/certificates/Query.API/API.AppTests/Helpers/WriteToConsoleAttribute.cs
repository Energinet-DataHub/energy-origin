using System;
using System.Reflection;
using Xunit.Sdk;

namespace API.AppTests.Helpers;

internal class WriteToConsoleAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        Console.WriteLine();
        Console.WriteLine($"[Starting {methodUnderTest.Name}]");
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Console.WriteLine($"[Finished {methodUnderTest.Name}]");
        Console.WriteLine();
    }
}
