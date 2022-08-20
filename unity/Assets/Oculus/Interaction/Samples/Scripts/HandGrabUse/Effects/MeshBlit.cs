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
using System.Collections;
using UnityEngine;

namespace Oculus.Interaction.Demo
{
    [RequireComponent(typeof(MeshFilter))]
    public class MeshBlit : MonoBehaviour
    {
        private static int MAIN_TEX = Shader.PropertyToID("_MainTex");

        public Material material;
        public RenderTexture renderTexture;

        [SerializeField]
        private float _blitsPerSecond = -1;
        public float BlitsPerSecond
        {
            get
            {
                return _blitsPerSecond;
            }
            set
            {
                SetBlitsPerSecond(value);
            }
        }

        private Mesh _mesh;
        private WaitForSeconds _waitForSeconds;

        private Mesh Mesh => _mesh ? _mesh : _mesh = GetComponent<MeshFilter>().sharedMesh;

        private void OnEnable()
        {
            SetBlitsPerSecond(_blitsPerSecond);
            StartCoroutine(BlitRoutine());

            IEnumerator BlitRoutine()
            {
                while (true)
                {
                    yield return _waitForSeconds;
                    Blit();
                }
            }
        }

        public void Blit()
        {
            if (renderTexture == null)
            {
                throw new NullReferenceException("MeshBlit.Blit must have a RenderTexture assigned");
            }
            if (material == null)
            {
                throw new NullReferenceException("MeshBlit.Blit must have a Material assigned");
            }
            if (Mesh == null)
            {
                throw new NullReferenceException("MeshBlit.Blit's MeshFilter has no mesh");
            }

            RenderTexture temp = RenderTexture.GetTemporary(renderTexture.descriptor);
            Graphics.Blit(renderTexture, temp);

            material.SetTexture(MAIN_TEX, temp);

            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            material.SetPass(0);
            Graphics.DrawMeshNow(Mesh, transform.localToWorldMatrix);
            RenderTexture.active = previous;

            material.SetTexture(MAIN_TEX, null);
            RenderTexture.ReleaseTemporary(temp);
        }

        private void SetBlitsPerSecond(float value)
        {
            _blitsPerSecond = value;
            _waitForSeconds = value > 0 ? new WaitForSeconds(1 / _blitsPerSecond) : null;
        }
    }
}
