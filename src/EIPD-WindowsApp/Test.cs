using System;
using System.Collections.Generic;

public class Test
{
    public string Series;
    public string DMC;
    public string DMCLink;
    public string Title;
    public string EIPDMatch;
    public string EIPDMatchLink;
    public string EIPDMatchTitle;
    public string Attempt;
    public string PartOfDMC { get; set; }
    public string Records { get; set; }
    public string WordsMatch { get; set; }
}


public class DMCT
{
    public string DMC { get; set; }
    public string DMCTitle { get; set; }
}