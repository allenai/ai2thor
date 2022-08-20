/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text.RegularExpressions;
using Facebook.WitAi.Lib;
using Facebook.WitAi.Utilities;
using UnityEngine;

namespace Facebook.WitAi.CallbackHandlers
{
    [AddComponentMenu("Wit.ai/Response Matchers/Utterance Matcher")]
    public class WitUtteranceMatcher : WitResponseHandler
    {
        [SerializeField] private string searchText;
        [SerializeField] private bool exactMatch = true;
        [SerializeField] private bool useRegex;

        [SerializeField] private StringEvent onUtteranceMatched = new StringEvent();

        private Regex regex;

        protected override void OnHandleResponse(WitResponseNode response)
        {
            var text = response["text"].Value;

            if (useRegex)
            {
                if (null == regex)
                {
                    regex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                }

                var match = regex.Match(text);
                if (match.Success)
                {
                    if (exactMatch && match.Value == text)
                    {
                        onUtteranceMatched?.Invoke(text);
                    }
                    else
                    {
                        onUtteranceMatched?.Invoke(text);
                    }
                }
            }
            else if (exactMatch && text.ToLower() == searchText.ToLower())
            {
                onUtteranceMatched?.Invoke(text);
            }
            else if (text.ToLower().Contains(searchText.ToLower()))
            {
                onUtteranceMatched?.Invoke(text);
            }
        }
    }
}
