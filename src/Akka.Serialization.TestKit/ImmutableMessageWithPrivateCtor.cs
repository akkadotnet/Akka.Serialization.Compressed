//-----------------------------------------------------------------------
// <copyright file="ImmutableMessageWithPrivateCtor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Serialization.TestKit;

public class ImmutableMessageWithPrivateCtorAutoProp: IEquatable<ImmutableMessageWithPrivateCtorAutoProp>
{
    public string? Foo { get; }
    public string? Bar { get; }

    protected ImmutableMessageWithPrivateCtorAutoProp()
    {
    }

    public bool Equals(ImmutableMessageWithPrivateCtorAutoProp? other)
    {
        return string.Equals(Bar, other?.Bar) && string.Equals(Foo, other?.Foo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is ImmutableMessageWithPrivateCtorAutoProp msg && Equals(msg);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Bar != null ? Bar.GetHashCode() : 0) * 397) ^ (Foo != null ? Foo.GetHashCode() : 0);
        }
    }

    public ImmutableMessageWithPrivateCtorAutoProp((string, string) nonConventionalArg)
    {
        Foo = nonConventionalArg.Item1;
        Bar = nonConventionalArg.Item2;
    }
}

public class ImmutableMessageWithPrivateCtor: IEquatable<ImmutableMessageWithPrivateCtor>
{
    public string? Foo { get; private set; } // Private setter test
    public string? Bar { get; private set; } // Private setter test

    protected ImmutableMessageWithPrivateCtor()
    {
    }

    public bool Equals(ImmutableMessageWithPrivateCtor? other)
    {
        return string.Equals(Bar, other?.Bar) && string.Equals(Foo, other?.Foo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is ImmutableMessageWithPrivateCtor msg && Equals(msg);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Bar != null ? Bar.GetHashCode() : 0) * 397) ^ (Foo != null ? Foo.GetHashCode() : 0);
        }
    }

    public ImmutableMessageWithPrivateCtor((string, string) nonConventionalArg)
    {
        Foo = nonConventionalArg.Item1;
        Bar = nonConventionalArg.Item2;
    }
}