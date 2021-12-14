using System;
using System.Collections.Generic;

public class Example1
{
    public IEnumerable<int> Interface { get; private set; }

    public Example1(IDisposable s, ICloneable c, int a, string str) { }

    public int Function1(int d, int e)
    {
        return 0;
    }
    public void Function2() { }
}

public class Example2
{
    public IEnumerable<int> Interface { get; private set; }

    public void Function1() { }
    public void Function2() { }
}