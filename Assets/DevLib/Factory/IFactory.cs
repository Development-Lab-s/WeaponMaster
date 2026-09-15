public interface IFactory<TProduct, TRequest>
{
    public TProduct Create(TRequest request);
}