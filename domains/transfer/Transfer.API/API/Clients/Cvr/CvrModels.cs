using System;
using System.Collections.Generic;

namespace API.Clients.Cvr;

public class NyesteNavn
{
    public string? navn { get; set; }
}

public class Root
{
    public int took { get; set; }
    public bool timed_out { get; set; }
    public HitsRoot? hits { get; set; }
}

public class HitsRoot
{
    public int total { get; set; }
    public List<Hit>? hits { get; set; }
}

public class Hit
{
    public Source? _source { get; set; }
}

public class Source
{
    public Vrvirksomhed? Vrvirksomhed { get; set; }
}

public class VirksomhedMetadata
{
    public NyesteNavn? nyesteNavn { get; set; }
    public NyesteBeliggenhedsadresse? nyesteBeliggenhedsadresse { get; set; }

}

public class Vrvirksomhed
{
    public int? cvrNummer { get; set; }
    public VirksomhedMetadata? virksomhedMetadata { get; set; }
}

public class NyesteBeliggenhedsadresse
{
    public string? landekode { get; set; }
    public string? fritekst { get; set; }
    public int? vejkode { get; set; }
    public Kommune? kommune { get; set; }
    public int? husnummerFra { get; set; }
    public Guid? adresseId { get; set; }
    public int? husnummerTil { get; set; }
    public string? bogstavFra { get; set; }
    public string? bogstavTil { get; set; }
    public string? etage { get; set; }
    public string? sidedoer { get; set; }
    public string? conavn { get; set; }
    public string? postboks { get; set; }
    public string? vejnavn { get; set; }
    public string? bynavn { get; set; }
    public int? postnummer { get; set; }
    public string? postdistrikt { get; set; }
}

public class Kommune
{
    public int? kommuneKode { get; set; }
    public string? kommuneNavn { get; set; }
}
