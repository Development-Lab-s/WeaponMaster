using System;
using System.Collections.Generic;

public static class Factories
{
    private static Dictionary<FactoryKey, object> _factories = new();

    public static void Register<TProduct, TRequest>(IFactory<TProduct, TRequest> factory)
    {
        if(factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _factories[FactoryKey.Of<TProduct, TRequest>()] = factory;
    }

    public static void Unregister<TProduct, TRequest>()
    {
        _factories.Remove(FactoryKey.Of<TProduct, TRequest>());
    }

    public static IFactory<TProduct, TRequest> GetFactory<TProduct, TRequest>()
    {
        object factory;

        if (!_factories.TryGetValue(FactoryKey.Of<TProduct, TRequest>(), out factory))
        {
            throw new KeyNotFoundException();
        }

        return factory as IFactory<TProduct, TRequest>;
    }
}