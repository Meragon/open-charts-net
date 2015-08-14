First, you should have a chart component on your form. If you don't have it, just copy a source code into your windows forms application and build it.

Simple chart code:

```
chart1.xAxis = new cxAxis[] 
{ 
   new cxAxis() { Categories = new string[] { "1", "2", "3", "4", "5" } } 
};
chart1.Series = new cSeries[] 
{
   new cSeries() { Name = "Series1", Data = new string[] { "0,1", "1", "4", "2" }, Type = cSeries.eType.Spline },
   new cSeries() { Name = "Series2", Data = new string[] { "1", "3", "2", null, "2,5" }, Type = cSeries.eType.Area }
};
```

Yep, thats all.