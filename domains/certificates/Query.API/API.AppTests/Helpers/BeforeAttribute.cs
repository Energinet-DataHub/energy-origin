using System;
using System.Reflection;
using Xunit.Sdk;

namespace API.AppTests.Helpers;

internal class BeforeAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest) => Console.WriteLine($"Starting {methodUnderTest.Name}");
}
