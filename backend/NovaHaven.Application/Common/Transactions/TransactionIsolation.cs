namespace NovaHaven.Application.Common.Transactions;

public enum TransactionIsolation
{
    ReadUncommitted,
    ReadCommitted,
    RepeatableRead,
    Serializable
}
