/*
 * SPDX-FileCopyrightText: © Hypermode Inc. <hello@hypermode.com>
 * SPDX-License-Identifier: Apache-2.0
 */

namespace Dgraph.Transactions
{
    public class Response
    {
        public readonly Api.Response DgraphResponse;

        private Lazy<Dictionary<string, string>> _Uids;

        internal Response(Api.Response dgraphResponse)
        {
            DgraphResponse = dgraphResponse;

            _Uids = new Lazy<Dictionary<string, string>>(
                () => new Dictionary<string, string>(DgraphResponse.Uids));
        }

        public string Json => DgraphResponse.Json.ToStringUtf8();

        public string Rdf => DgraphResponse.Rdf.ToStringUtf8();

        public Dictionary<string, string> Uids => _Uids.Value;
    }
}
