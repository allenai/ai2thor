/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using UnityEngine;
using System;

namespace Oculus.Interaction
{
    /// <summary>
    /// When this attribute is attached to a MonoBehaviour field within a
    /// Unity Object, this allows an interface to be specified in to to
    /// entire only a specific type of MonoBehaviour can be attached.
    /// </summary>
    public class InterfaceAttribute : PropertyAttribute
    {
        public Type[] Types = null;
        public string TypeFromFieldName;

        /// <summary>
        /// Creates a new Interface attribute.
        /// </summary>
        /// <param name="type">The type of interface which is allowed.</param>
        public InterfaceAttribute(Type type, params Type[] types)
        {
            Types = new Type[types.Length + 1];
            Types[0] = type;
            for (int i = 0; i < types.Length; i++)
            {
                Types[i + 1] = types[i];
            }
        }

        public InterfaceAttribute(string typeFromFieldName)
        {
            this.TypeFromFieldName = typeFromFieldName;
        }
    }
}
