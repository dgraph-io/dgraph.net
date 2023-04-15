using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dgraph.tests.e2e.Orchestration
{
    public interface IDgraphClientFactory
    {
        Task<IDgraphClient> GetDgraphClient();
    }
}