namespace Wynathan
{
    public interface ICopyable<T>
    {
        T ShallowCopy();

        T DeepCopy();
    }
}
