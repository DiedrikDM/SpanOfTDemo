# SpanOfTDemo
This is a Demo to show the possibilities of Span&lt;T>
I'll include the accompanying Blog Post which you can find [here](https://blogs.u2u.be/diedrik/post/span-t-the-what-why-and-how)

# Span&lt;T> What, Why and How?

The Span&lt;T> is a value type added since C# 7.2. This was released in November 2017, so it should be a known feature by now... However not a lot of people actually know what it does, why it was added or how you use it. Let's have a look.

## What is Span&lt;T>?

> Provides a type- and memory-safe representation of a contiguous region of arbitrary memory. 
[View Docs](https://docs.microsoft.com/en-us/dotnet/api/system.span-1?view=netcore-2.1)

System.Span&lt;T> is a value type that can represent adjacent regions of memory. It doesn't care whether it's used on managed objects, interop objects or objects on the stack. The Span allows for safe access to those kinds of objects while still providing the performance similar to arrays.

In other words, whether you create a string, a char-array or an IntPtr that refers to a string; there's no difference for a Span&lt;T>. It will allow you to reference all three structures transparently and it is able to access them in a similar time as an array.

Let's look at an example.
## Parsing a string

Suppose we would like to implement our own web server. In our own naive implementation we want to grab the first line of an HTTP request message and split it up into its parts. So that we can get the HTTP Verb, the resource and the HTTP version from it.

```C#
string httpRequest = "GET /css/styles.css HTTP/1.1";
string[] parts = httpRequest.Split();
string method = parts[0];
string resource = parts[1];
string httpVersion = parts[2];
RetrieveResourceAndRespond(method, resource, httpVersion);
```
This code will accomplish exactly that. How we process the parts in the function, is up to the developer. The code could be optimized and secured a bit further, but it serves as a great example.

### What is bad about this code?
The code that we're using here will instantiate a new string for every part that you separate and it will create a new array with all of these parts in it. This will add some extra overhead however, and every CPU cycle lost in this sense could be very problematic on a web server. 
> See at the bottom of this article for average timings

### Can we improve this?
Well, if we avoid creating a new array everytime to hold all of the parts, the speed will increase. The next implementation could again be secured a bit further, but here we go.

```C#
string httpRequest = "GET /css/styles.css HTTP/1.1";
int indexOfFirst = httpRequest.IndexOf(' ');
string method = httpRequest.Substring(0, indexOfFirst);
int indexOfLast = httpRequest.LastIndexOf(' ');
string resource = httpRequest.Substring(indexOfFirst + 1, indexOfLast - indexOfFirst);
string httpVersion = httpRequest.Substring(indexOfLast);
RetrieveResourceAndRespond(method, resource, httpVersion);
```
### What is bad about this code?
Because we make use of the _IndexOf_ operations and then the _Substring_ method to get our parts, the creation of the array is avoided. Like I already said, the speed is heavily increased by doing this. However, it will create a new string everytime you return the part from the _Substring_ method. And again we lose CPU cycles in creating those strings. I'll include timings at the bottom of this article.

### Using Span&lt;T>
Span&lt;T> allows us to create a kind of _safe to use_ pointer towards segments in memory. Now don't start cringing because you read the word pointer. It is __luckily__ not exactly that! Span&lt;T> let's us take a string, look at it as an array of characters and select sub elements in a very transparent way. I'll show you:
```C#
ReadOnlySpan<char> httpRequest = "GET /css/styles.css HTTP/1.1".AsSpan();
int indexOfFirst = httpRequest.IndexOf(' ');
ReadOnlySpan<char> method = httpRequest.Slice(0, indexOfFirst);
int indexOfLast = httpRequest.LastIndexOf(' ');
ReadOnlySpan<char> resource = httpRequest.Slice(indexOfFirst + 1, indexOfLast - indexOfFirst);
ReadOnlySpan<char> httpVersion = httpRequest.Slice(indexOfLast);
RetrieveResourceAndRespond(method, resource, httpVersion);
```
### Improvements
This code allows us to improve the speed even further. Every time the Span&lt;T> gets _sliced_ it will actually create a reference to that new sub Span&lt;T>. In other words, we're allocating memory each time, instead we're adding references; _pointers_ if you will; to the application. See the timings at the bottom of the article.

### Disadvantages
Well you need to think about your API on a much lower level all of a sudden. We're not using strings anymore, but arrays of characters. They are intrinsically very similar but they're clearly not the same. 

## Why is this used?
Here are a couple of other advantages except for the clear difference in speed. If you've ever worked with __IntPtr__ before, you know that you need to wrap it into an _unsafe_ block and then make use of those objects as you would in C/C++. With a Span&lt;T> you don't need to worry about these unsafe mechanisms, your code becomes a lot more maintainable.
Another advantage is the Span&lt;T> struct only cares about the continuous block in memory to represent the data, not about the wrapping type (Array, interop objects, IntPtr), which makes our code a bit more generic.

## Where is it used?
Currently Microsoft is making use of Span&lt;T> to add lots of performance gains in the .NET Core libraries. You can find an example at [this url](https://github.com/dotnet/corefx/blob/master/src/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/ArrayBuffer.cs). This is used in the System.Net.Http library, where parsing strings is of course of the outmost importance!

## Timings
I've created the following simple program to create average timings when calling Split, Substring or Slice for strings and/or Span&lt;T>. I made sure to allow the methods to be warmed up in every cycle (calling it one time without the Stopwatch started). I also included an overview of the amount of objects Garbage Collected after every (inner) loop.
```C#
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpanOfT
{
  internal class Program
  {
    private static void Main(string[] args)
    {
      var elapsedMsSplit = new List<long>();
      var elapsedMsSlice = new List<long>();
      var elapsedMsSubstring = new List<long>();
      int counter = 10;
      for (int j = 0; j < counter; j++)
      {
        Stopwatch sw = new Stopwatch();

        int numberOfGCs = GC.CollectionCount(0);

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
        sw.Stop();
        elapsedMsSplit.Add(sw.ElapsedMilliseconds);
        int numberOfGCsAfter = GC.CollectionCount(0);
        Console.WriteLine("GC Split: "+(numberOfGCsAfter - numberOfGCs));

        numberOfGCs = GC.CollectionCount(0);
        sw.Reset();
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
        sw.Stop();
        elapsedMsSubstring.Add(sw.ElapsedMilliseconds);
        numberOfGCsAfter = GC.CollectionCount(0);
        Console.WriteLine("GC Substring: " + (numberOfGCsAfter - numberOfGCs));

        numberOfGCs = GC.CollectionCount(0);
        sw.Reset();
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
        sw.Stop();
        elapsedMsSlice.Add(sw.ElapsedMilliseconds);
        numberOfGCsAfter = GC.CollectionCount(0);
        Console.WriteLine("GC Slice: " + (numberOfGCsAfter - numberOfGCs));
      }

      Console.WriteLine($"split avg: {elapsedMsSplit.Average()}, slice avg: {elapsedMsSlice.Average()}, substring avg: {elapsedMsSubstring.Average()}");
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


```


This gave me following timings in Debug mode - with Debugger attached:
|Method|Run 1 (ms)|Run 2 (ms)|Run 3 (ms)|Run 4 (ms)|Run 5 (ms)|Run 6 (ms)|Run 7 (ms)|Run 8 (ms)|Run 9 (ms)|Run 10 (ms)|Average (ms)|
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
|Split|3762|3260|3302|3366|355|3301|3330|3587|3417|3311|3421,1|
|Substring|1240|1262|1485|1259|1674|1269|1343|1310|1313|1234|1338,9|
|Slice|913|915|1006|919|927|958|939|944|943|947|941,1|

I also tested it without Debugger attached and in Release mode, these gave me the following average values:

|Method|Debugger (avg ms)|No Debugger (avg ms)|Release (avg ms)|
|---|---:|---:|---:|
|Split|3421,1|3120,8|3068,6|
|Substring|1338,9|1198,9|1022,7|
|Slice|941,1|898,7|234,8|

The amount of objects that are Garbage Collected after every loop gave an interesting number as well:
|Method|Average number of objects GC'd|
|---|---:|
|Split|1170|
|Substring|915|
|Slice|0|

>__NOTE:__ I do believe the Release build with the current implementation for method _RetrieveResourceAndRespond_ could have removed quite a lot of code in the IL representation. I did not check this.



## The End?
With this example I only explained Span&lt;T>, there are actually quite a lot of extra constructs that were added in the last C# versions. In a later blog post I'll highlight a couple of those as well.



