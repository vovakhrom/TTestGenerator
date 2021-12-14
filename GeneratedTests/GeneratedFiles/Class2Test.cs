using System;
using NUnit.Framework;
using MainPart.Files;
using Moq;
using System.Collections.Generic;

[TestFixture]
class Class2Test
{
    private Class2 _class2;
    [SetUp]
    public void SetUp()
    {
        _class2 = new Class2();
    }
}