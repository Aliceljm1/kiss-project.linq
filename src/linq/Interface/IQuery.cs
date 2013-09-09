namespace Kiss.Linq
{
    public interface IQuery<T>
    {
        T Single();
        T SingleOrDefault();
        T First();
        T FirstOrDefault();
        T Last();
        T LastOrDefault();
    }

    public interface IQuery
    {
        bool Any();
        object Count();
    }
}