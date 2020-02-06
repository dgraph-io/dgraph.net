

namespace DgraphDotNet.Transactions
{
    internal interface ITransactionFactory {
        ITransaction NewTransaction(DgraphClient client);
    }

    internal class TransactionFactory : ITransactionFactory
    {
        public ITransaction NewTransaction(DgraphClient client) {
            return new Transaction(client);
        }
    }
}