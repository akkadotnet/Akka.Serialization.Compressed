//-----------------------------------------------------------------------
// <copyright file="ImmutableMessage.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Serialization.TestKit;

public class ImmutableMessageAutoProp: IEquatable<ImmutableMessageAutoProp>
{
    public string? Foo { get; } 
    public string? Bar { get; } 

    public ImmutableMessageAutoProp()
    {
    }

    public ImmutableMessageAutoProp((string, string) nonConventionalArg)
    {
        Foo = nonConventionalArg.Item1;
        Bar = nonConventionalArg.Item2;
    }

    public bool Equals(ImmutableMessageAutoProp? other)
    {
        return string.Equals(Bar, other?.Bar) && string.Equals(Foo, other?.Foo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is ImmutableMessageAutoProp msg && Equals(msg);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Bar != null ? Bar.GetHashCode() : 0) * 397) ^ (Foo != null ? Foo.GetHashCode() : 0);
        }
    }
}

public class ImmutableMessage: IEquatable<ImmutableMessage>
{
    public string? Foo { get; private set; } // Private setter is required for JSON to work
    public string? Bar { get; private set; } // Private setter is required for JSON to work

    public ImmutableMessage()
    {
    }

    public ImmutableMessage((string, string) nonConventionalArg)
    {
        Foo = nonConventionalArg.Item1;
        Bar = nonConventionalArg.Item2;
    }

    public bool Equals(ImmutableMessage? other)
    {
        return string.Equals(Bar, other?.Bar) && string.Equals(Foo, other?.Foo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is ImmutableMessage msg && Equals(msg);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Bar != null ? Bar.GetHashCode() : 0) * 397) ^ (Foo != null ? Foo.GetHashCode() : 0);
        }
    }
}