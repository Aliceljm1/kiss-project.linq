namespace Kiss.Linq
{
    interface IVersionItem
    {
        void Commit ( );
        void Revert ( );
        object Item { get; }
    }
}
