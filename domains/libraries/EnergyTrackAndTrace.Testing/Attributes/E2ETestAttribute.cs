using System;
using System.Collections.Generic;
using Xunit.v3;

namespace EnergyTrackAndTrace.Testing.Attributes;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class E2ETestAttribute : Attribute, ITraitAttribute
{
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => [new("Category", "E2ETest")];
}
