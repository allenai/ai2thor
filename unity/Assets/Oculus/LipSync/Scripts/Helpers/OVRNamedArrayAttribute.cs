/************************************************************************************
Filename    :   OVRNamedArrayAttribute.cs
Content     :   Adds support for a named array attribute in the editor
Created     :   May 17th, 2018
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEngine;

// Adds support for a named array attribute in the editor
public class OVRNamedArrayAttribute : PropertyAttribute {
    public readonly string[] names;
    public OVRNamedArrayAttribute( string[] names ) { this.names = names; }
}
