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

using System.Collections.Generic;

namespace Oculus.Voice
{
    public static class AppBuiltIns
    {
        public static string builtInPrefix = "builtin:";
        private static string modelName = "Built-in Models";

        public static readonly Dictionary<string, Dictionary<string, string>>
            apps = new Dictionary<string, Dictionary<string, string>>
            {
                {
                    "Chinese", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_zh"},
                        {"name", modelName},
                        {"lang", "zh"},
                        {"clientToken", "3KQH33637TAT7WD4TG7T65SDRO73WZGY"},
                    }
                },
                {
                    "Dutch", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_nl"},
                        {"name", modelName},
                        {"lang", "nl"},
                        {"clientToken", "ZCD6HCNCL6GTJKZ3QKWNKQVEDI4GUL7C"},
                    }
                },
                {
                    "English", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_en"},
                        {"name", modelName},
                        {"lang", "en"},
                        {"clientToken", "HOKEABS7HPIQVSRSVWRPTTV75TQJ5QBP"},
                    }
                },
                {
                    "French", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_fr"},
                        {"name", modelName},
                        {"lang", "fr"},
                        {"clientToken", "7PP7NK2QAH67MREGZV6SB6RIEWAYDNRY"},
                    }
                },
                {
                    "German", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_de"},
                        {"name", modelName},
                        {"lang", "de"},
                        {"clientToken", "7LXOOB4JC7MXPUTTGQHDVQMHGEEJT6LE"},
                    }
                },
                {
                    "Italian", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_it"},
                        {"name", modelName},
                        {"lang", "it"},
                        {"clientToken", "KELCNR4DCCPPOCF2RDFS4M6JOCWWIFII"},
                    }
                },
                {
                    "Japanese", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_ja"},
                        {"name", modelName},
                        {"lang", "ja"},
                        {"clientToken", "TPJGLBBCHJ5F7BVVN5XLEGP6YDQRUE3P"},
                    }
                },
                {
                    "Korean", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_ko"},
                        {"name", modelName},
                        {"lang", "ko"},
                        {"clientToken", "NT4WJLL7ACMFBXS4B7W5GRLTKDZQ36R4"},
                    }
                },
                {
                    "Polish", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_pl"},
                        {"name", modelName},
                        {"lang", "pl"},
                        {"clientToken", "DMDRHGYDYN33D3IKCX5BG5R57EL2IIC4"},
                    }
                },
                {
                    "Portuguese", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_pt"},
                        {"name", modelName},
                        {"lang", "pt"},
                        {"clientToken", "W4W3BSKL72HZC5MXLILONJUCG732SQQN"},
                    }
                },
                {
                    "Russian", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_ru"},
                        {"name", modelName},
                        {"lang", "ru"},
                        {"clientToken", "W67HLUWA3MBYVEKRW3VVWUKSNZGAOFBI"},
                    }
                },
                {
                    "Spanish", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_es"},
                        {"name", modelName},
                        {"lang", "es"},
                        {"clientToken", "YW7AM5OOVSW5XKGYKFE2S2HLC2WHC3UI"},
                    }
                },
                {
                    "Swedish", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_sv"},
                        {"name", modelName},
                        {"lang", "sv"},
                        {"clientToken", "NPE3UJ7Y4NIVTUOZ7QPPAP3TY6FYPXJY"},
                    }
                },
                {
                    "Turkish", new Dictionary<string, string>
                    {
                        {"id", "voiceSDK_tr"},
                        {"name", modelName},
                        {"lang", "tr"},
                        {"clientToken", "ZCISEDXESLYJOROLNOODCGGPZXHLUAEE"},
                    }
                },
            };

        public static string[] appNames
        {
            get
            {
                string[] keys = new string[apps.Keys.Count];
                apps.Keys.CopyTo(keys, 0);
                return keys;
            }
        }
    }
}
