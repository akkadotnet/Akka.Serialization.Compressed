﻿//-----------------------------------------------------------------------
// <copyright file="UntypedContainerMessage.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2021 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2021 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Serialization.TestKit
{
    public class UntypedContainerMessage : IEquatable<UntypedContainerMessage>
    {
        public bool Equals(UntypedContainerMessage? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Contents, other.Contents);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is UntypedContainerMessage msg && Equals(msg);
        }

        public override int GetHashCode()
        {
            return Contents != null ? Contents.GetHashCode() : 0;
        }

        public static bool operator ==(UntypedContainerMessage left, UntypedContainerMessage right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(UntypedContainerMessage left, UntypedContainerMessage right)
        {
            return !Equals(left, right);
        }

        public object? Contents { get; set; }

        public override string ToString()
        {
            return $"<UntypedContainerMessage {Contents}>";
        }
    }
}
