/*
 * Copyright 2020 Dgraph Labs, Inc. and Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Api;
using Google.Protobuf;

namespace Dgraph.Transactions
{

    public class MutationBuilder {

        internal Mutation Mutation = new Mutation();
        
        public string SetJson {
            get {
                return Mutation.SetJson.ToStringUtf8();
            }
            set {
                Mutation.SetJson = ByteString.CopyFromUtf8(value ?? "");
            }
        }
        public string SetNquads {
            get {
                return Mutation.SetNquads.ToStringUtf8();
            }
            set {
                Mutation.SetNquads = ByteString.CopyFromUtf8(value ?? "");
            }            
        }
        public string DeleteJson {
            get {
                return Mutation.DeleteJson.ToStringUtf8();
            }
            set {
                Mutation.DeleteJson = ByteString.CopyFromUtf8(value ?? "");
            }               
        }

        public string DelNquads {
            get {
                return Mutation.DelNquads.ToStringUtf8();
            }
            set {
                Mutation.DelNquads = ByteString.CopyFromUtf8(value ?? "");
            }   
        }

        public string Cond {
            get {
                return Mutation.Cond;
            }
            set {
                Mutation.Cond = value ?? "";
            }  
        }

    }

}
