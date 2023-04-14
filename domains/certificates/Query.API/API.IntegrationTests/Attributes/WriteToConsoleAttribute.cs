using System;
using System.Reflection;
using Xunit.Sdk;

namespace API.IntegrationTests.Attributes;

internal class WriteToConsoleAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
#if CI
        Console.WriteLine("Running on CI");
#else
        Console.WriteLine("Not running on CI");
#endif
        Console.WriteLine();
        Console.WriteLine($"[Starting {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Console.WriteLine($"[Finished {methodUnderTest.DeclaringType}.{methodUnderTest.Name}]");
        Console.WriteLine();
    }
}
