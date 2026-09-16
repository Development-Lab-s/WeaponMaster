using System;

public readonly struct FactoryKey : IEquatable<FactoryKey>
{
    private readonly Type _product;
    private readonly Type _request;

    public static FactoryKey Of<TProduct, TRequest>()
    {
        return new(typeof(TProduct), typeof(TRequest));
    }

    private FactoryKey(Type p, Type r)
    {
        _product = p;
        _request = r;
    }

    public bool Equals(FactoryKey other)
    {
        return _product.Equals(other._product) && _request.Equals(other._request);
    }

    public override bool Equals(object obj)
    {
        return obj is FactoryKey o && Equals(o);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_product.GetHashCode(), _request.GetHashCode());
    }
}