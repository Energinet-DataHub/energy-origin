using System;
using System.Reflection;
using Xunit.Sdk;

namespace API.AppTests.Infrastructure.WriteToConsole;

internal class WriteToConsoleAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        Console.WriteLine();
        Console.WriteLine($"[Starting {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Console.WriteLine($"[Finished {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
        Console.WriteLine();
    }
}
