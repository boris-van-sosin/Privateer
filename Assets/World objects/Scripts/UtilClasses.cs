using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Tuple<T1, T2>
{
    public Tuple(T1 first, T2 second)
    {
        Item1 = first;
        Item2 = second;
    }

    public static Tuple<T1, T2> Create(T1 first, T2 second)
    {
        return new Tuple<T1, T2>(first, second);
    }

    public override bool Equals(object obj)
    {
        Tuple<T1, T2> other = obj as Tuple<T1, T2>;

        if (other == null)
        {
            return false;
        }

        return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2);
    }

    public override int GetHashCode()
    {
        return Item1.GetHashCode() ^ Item2.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", Item1, Item2);
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
}

public sealed class Tuple<T1, T2, T3>
{
    public Tuple(T1 first, T2 second, T3 third)
    {
        Item1 = first;
        Item2 = second;
        Item3 = third;
    }

    public static Tuple<T1, T2, T3> Create(T1 first, T2 second, T3 third)
    {
        return new Tuple<T1, T2, T3>(first, second, third);
    }

    public override bool Equals(object obj)
    {
        Tuple<T1, T2, T3> other = obj as Tuple<T1, T2, T3>;

        if (other == null)
        {
            return false;
        }

        return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2) && this.Item3.Equals(other.Item3);
    }

    public override int GetHashCode()
    {
        return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("({0},{1},{2})", Item1, Item2, Item3);
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
}

public struct ValueTuple<T1, T2>
{
    public ValueTuple(T1 first, T2 second)
    {
        Item1 = first;
        Item2 = second;
    }

    public static ValueTuple<T1, T2> Create(T1 first, T2 second)
    {
        return new ValueTuple<T1, T2>(first, second);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ValueTuple<T1, T2>))
        {
            return false;
        }
        ValueTuple<T1, T2> other = (ValueTuple<T1, T2>)obj;

        return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2);
    }

    public override int GetHashCode()
    {
        return Item1.GetHashCode() ^ Item2.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("({0},{1})", Item1, Item2);
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
}

public struct ValueTuple<T1, T2, T3>
{
    public ValueTuple(T1 first, T2 second, T3 third)
    {
        Item1 = first;
        Item2 = second;
        Item3 = third;
    }

    public static ValueTuple<T1, T2, T3> Create(T1 first, T2 second, T3 third)
    {
        return new ValueTuple<T1, T2, T3>(first, second, third);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ValueTuple<T1, T2, T3>))
        {
            return false;
        }
        ValueTuple<T1, T2, T3> other = (ValueTuple<T1, T2, T3>)obj;

        return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2) && this.Item3.Equals(other.Item3);
    }

    public override int GetHashCode()
    {
        return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
    }

    public override string ToString()
    {
        return string.Format("({0},{1},{2})", Item1, Item2, Item3);
    }

    public readonly T1 Item1;
    public readonly T2 Item2;
    public readonly T3 Item3;
}
