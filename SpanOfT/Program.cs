using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpanOfT
{
  internal class Program
  {

    private static Benchmark AddBenchmarks(Action<Stopwatch> action)
    {
      Stopwatch sw = new Stopwatch();
      int numberOfGCs = GC.CollectionCount(0);

      action(sw);

      sw.Stop();
      int numberOfGCsAfter = GC.CollectionCount(0);
      return new Benchmark { TimeSpentInMs = sw.ElapsedMilliseconds, NumberOfGenerationZeroCollected = numberOfGCsAfter - numberOfGCs };
    }

    private static void Main(string[] args)
    {
      var elapsedMsSplit = new List<long>();
      var elapsedMsSlice = new List<long>();
      var elapsedMsSubstring = new List<long>();
      int counter = 10;
      for (int j = 0; j < counter; j++)
      {
        var result = AddBenchmarks(StringWithSplit);
        elapsedMsSplit.Add(result.TimeSpentInMs);
        Console.WriteLine("GC Split: " + result.NumberOfGenerationZeroCollected);

        result = AddBenchmarks(StringWithSubstring);
        elapsedMsSubstring.Add(result.TimeSpentInMs);
        Console.WriteLine("GC Substring: " + result.NumberOfGenerationZeroCollected);

        result = AddBenchmarks(SpanWithSlice);
        elapsedMsSlice.Add(result.TimeSpentInMs);
        Console.WriteLine("GC Slice: " + result.NumberOfGenerationZeroCollected);
      }

      Console.WriteLine($"split avg: {elapsedMsSplit.Average()}, slice avg: {elapsedMsSlice.Average()}, substring avg: {elapsedMsSubstring.Average()}");
    }

    private static void StringWithSplit(Stopwatch sw)
    {
      for (int i = 0; i <= 20_000_000; i++)
      {
        if (i == 1) { sw.Start(); } // first time is a warmup
        string httpRequest = "GET /css/styles.css HTTP/1.1";
        string[] parts = httpRequest.Split();
        string method = parts[0];
        string resource = parts[1];
        string httpVersion = parts[2];
        RetrieveResourceAndRespond(method, resource, httpVersion);
      }
    }

    private static void StringWithSubstring(Stopwatch sw)
    {
      for (int i = 0; i <= 20_000_000; i++)
      {
        if (i == 1) { sw.Start(); } // first time is a warmup
        string httpRequest = "GET /css/styles.css HTTP/1.1";
        int indexOfFirst = httpRequest.IndexOf(' ');
        string method = httpRequest.Substring(0, indexOfFirst);
        int indexOfLast = httpRequest.LastIndexOf(' ');
        string resource = httpRequest.Substring(indexOfFirst + 1, indexOfLast - indexOfFirst);
        string httpVersion = httpRequest.Substring(indexOfLast);
        RetrieveResourceAndRespond(method, resource, httpVersion);
      }
    }

    private static void SpanWithSlice(Stopwatch sw)
    {
      for (int i = 0; i <= 20_000_000; i++)
      {
        if (i == 1) { sw.Start(); } // first time is a warmup
        ReadOnlySpan<char> httpRequest = "GET /css/styles.css HTTP/1.1".AsSpan();
        int indexOfFirst = httpRequest.IndexOf(' ');
        ReadOnlySpan<char> method = httpRequest.Slice(0, indexOfFirst);
        int indexOfLast = httpRequest.LastIndexOf(' ');
        ReadOnlySpan<char> resource = httpRequest.Slice(indexOfFirst + 1, indexOfLast - indexOfFirst);
        ReadOnlySpan<char> httpVersion = httpRequest.Slice(indexOfLast);
        RetrieveResource(method, resource, httpVersion);
      }
    }

    private static void RetrieveResourceAndRespond(string method, string resource, string httpVersion)
    {
      //
    }

    private static void RetrieveResource(ReadOnlySpan<char> method, ReadOnlySpan<char> resource, ReadOnlySpan<char> httpVersion)
    {

    }
  }
}
