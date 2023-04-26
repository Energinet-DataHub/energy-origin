using System;

namespace API.IntegrationTests.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }

    public TestPriorityAttribute(int priority) => Priority = priority;
}
