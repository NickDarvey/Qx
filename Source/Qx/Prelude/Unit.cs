using System;

namespace Qx.Prelude
{
    /// <summary>
    /// A void-like type that is used in Qx for closing unneeded generic type parameters.
    /// </summary>
    public class Unit : IEquatable<Unit>, IComparable<Unit>
    {
        public static readonly Unit Default = new Unit();

        private Unit() { }

        public override int GetHashCode() => 0;

        public override bool Equals(object obj) => obj is Unit;

        public bool Equals(Unit other) => true;

        public int CompareTo(Unit other) => 0;

        public static bool operator ==(Unit lhs, Unit rhs) => true;

        public static bool operator !=(Unit lhs, Unit rhs) => false;
    }
}
