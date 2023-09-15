using System.Collections.Generic;

namespace API.Cvr.Models;

public class NyesteNavn
{
    public string navn { get; set; }
}

public class Root
{
    public int took { get; set; }
    public bool timed_out { get; set; }
    public HitsRoot hits { get; set; }
}

public class HitsRoot
{
    public int total { get; set; }
    public List<Hit> hits { get; set; }
}

public class Hit
{
    public Source _source { get; set; }
}

public class Source
{
    public Vrvirksomhed Vrvirksomhed { get; set; }
}

public class VirksomhedMetadata
{
    public NyesteNavn nyesteNavn { get; set; }
}

public class Vrvirksomhed
{
    public int cvrNummer { get; set; }
    public VirksomhedMetadata virksomhedMetadata { get; set; }
}
