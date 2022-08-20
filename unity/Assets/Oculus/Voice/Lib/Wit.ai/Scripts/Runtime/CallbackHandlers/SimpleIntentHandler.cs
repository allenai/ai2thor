/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Lib;
using UnityEngine;
using UnityEngine.Events;

namespace Facebook.WitAi.CallbackHandlers
{
    [AddComponentMenu("Wit.ai/Response Matchers/Simple Intent Handler")]
    public class SimpleIntentHandler : WitResponseHandler
    {
        [SerializeField] public string intent;
        [Range(0, 1f)]
        [SerializeField] public float confidence = .9f;
        [SerializeField] private UnityEvent onIntentTriggered = new UnityEvent();

        [Tooltip("Confidence ranges are executed in order. If checked, all confidence values will be checked instead of stopping on the first one that matches.")]
        [SerializeField] public bool allowConfidenceOverlap;
        [SerializeField] public ConfidenceRange[] confidenceRanges;

        public UnityEvent OnIntentTriggered => onIntentTriggered;

        protected override void OnHandleResponse(WitResponseNode response)
        {
            if (null == response) return;

            bool matched = false;
            foreach (var intentNode in response?["intents"]?.Childs)
            {
                var resultConfidence = intentNode["confidence"].AsFloat;
                if (intent == intentNode["name"].Value)
                {
                    matched = true;
                    if (resultConfidence >= confidence)
                    {
                        onIntentTriggered.Invoke();
                    }

                    CheckInsideRange(resultConfidence);
                    CheckOutsideRange(resultConfidence);
                }
            }

            if(!matched)
            {
                CheckInsideRange(0);
                CheckOutsideRange(0);
            }
        }

        private void CheckOutsideRange(float resultConfidence)
        {
            for (int i = 0; null != confidenceRanges && i < confidenceRanges.Length; i++)
            {
                var range = confidenceRanges[i];
                if (resultConfidence < range.minConfidence ||
                    resultConfidence > range.maxConfidence)
                {
                    range.onOutsideConfidenceRange?.Invoke();

                    if (!allowConfidenceOverlap) break;
                }
            }
        }

        private void CheckInsideRange(float resultConfidence)
        {
            for (int i = 0; null != confidenceRanges && i < confidenceRanges.Length; i++)
            {
                var range = confidenceRanges[i];
                if (resultConfidence >= range.minConfidence &&
                    resultConfidence <= range.maxConfidence)
                {
                    range.onWithinConfidenceRange?.Invoke();

                    if (!allowConfidenceOverlap) break;
                }
            }
        }
    }
}
