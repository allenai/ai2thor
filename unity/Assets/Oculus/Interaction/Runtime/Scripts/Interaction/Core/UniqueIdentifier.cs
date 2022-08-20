/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    public class UniqueIdentifier
    {
        public int ID { get; private set; }


        private UniqueIdentifier(int identifier)
        {
            ID = identifier;
        }

        private static System.Random Random = new System.Random();
        private static HashSet<int> _identifierSet = new HashSet<int>();

        public static UniqueIdentifier Generate()
        {
            while (true)
            {
                int identifier = Random.Next(Int32.MaxValue);
                if (_identifierSet.Contains(identifier)) continue;
                _identifierSet.Add(identifier);
                return new UniqueIdentifier(identifier);
            }
        }

        public static void Release(UniqueIdentifier identifier)
        {
            _identifierSet.Remove(identifier.ID);
        }
    }
}
