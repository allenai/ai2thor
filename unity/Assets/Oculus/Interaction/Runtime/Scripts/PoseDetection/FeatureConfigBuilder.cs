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

namespace Oculus.Interaction.PoseDetection
{
    using FingerFeatureConfig = ShapeRecognizer.FingerFeatureConfig;

    public class FeatureConfigBuilder
    {
        public class BuildCondition<TBuildState>
        {
            private readonly BuildStateDelegate _buildStateFn;

            public delegate TBuildState BuildStateDelegate(FeatureStateActiveMode mode);

            public BuildCondition(BuildStateDelegate buildStateFn)
            {
                _buildStateFn = buildStateFn;
            }
            public TBuildState Is => _buildStateFn(FeatureStateActiveMode.Is);

            public TBuildState IsNot => _buildStateFn(FeatureStateActiveMode.IsNot);
        }
    }

    public class FingerFeatureConfigBuilder : FeatureConfigBuilder
    {
        public static BuildCondition<OpenCloseStateBuilder> Curl { get; } =
            new BuildCondition<OpenCloseStateBuilder>(mode => new OpenCloseStateBuilder(mode, FingerFeature.Curl));
        public static BuildCondition<OpenCloseStateBuilder> Flexion  { get; } =
            new BuildCondition<OpenCloseStateBuilder>(mode => new OpenCloseStateBuilder(mode, FingerFeature.Flexion));
        public static BuildCondition<AbductionStateBuilder> Abduction { get; } =
            new BuildCondition<AbductionStateBuilder>(mode => new AbductionStateBuilder(mode));
        public static BuildCondition<OppositionStateBuilder> Opposition { get; } =
            new BuildCondition<OppositionStateBuilder>(mode => new OppositionStateBuilder(mode));

        public class OpenCloseStateBuilder
        {
            private readonly FeatureStateActiveMode _mode;
            private readonly FingerFeature _fingerFeature;
            private readonly FeatureStateDescription[] _states;

            public OpenCloseStateBuilder(FeatureStateActiveMode featureStateActiveMode,
                FingerFeature fingerFeature)
            {
                _mode = featureStateActiveMode;
                _fingerFeature = fingerFeature;
                _states = FingerFeatureProperties.FeatureDescriptions[_fingerFeature].FeatureStates;
            }

            public FingerFeatureConfig Open =>
                new FingerFeatureConfig { Feature = _fingerFeature, Mode = _mode, State = _states[0].Id };
            public FingerFeatureConfig Neutral =>
                new FingerFeatureConfig { Feature = _fingerFeature, Mode = _mode, State = _states[1].Id };
            public FingerFeatureConfig Closed =>
                new FingerFeatureConfig { Feature = _fingerFeature, Mode = _mode, State = _states[2].Id };
        }

        public class AbductionStateBuilder
        {
            private readonly FeatureStateActiveMode _mode;

            public AbductionStateBuilder(FeatureStateActiveMode mode)
            {
                _mode = mode;
            }
            public FingerFeatureConfig None =>
                new FingerFeatureConfig { Feature = FingerFeature.Abduction, Mode = _mode, State = FingerFeatureProperties.AbductionFeatureStates[0].Id };
            public FingerFeatureConfig Closed =>
                new FingerFeatureConfig { Feature = FingerFeature.Abduction, Mode = _mode, State = FingerFeatureProperties.AbductionFeatureStates[1].Id };
            public FingerFeatureConfig Open =>
                new FingerFeatureConfig { Feature = FingerFeature.Abduction, Mode = _mode, State = FingerFeatureProperties.AbductionFeatureStates[2].Id };
        }

        public class OppositionStateBuilder
        {
            private readonly FeatureStateActiveMode _mode;

            public OppositionStateBuilder(FeatureStateActiveMode mode)
            {
                _mode = mode;
            }
            public FingerFeatureConfig Touching =>
                new FingerFeatureConfig { Feature = FingerFeature.Opposition, Mode = _mode, State = FingerFeatureProperties.OppositionFeatureStates[0].Id };
            public FingerFeatureConfig Near =>
                new FingerFeatureConfig { Feature = FingerFeature.Opposition, Mode = _mode, State = FingerFeatureProperties.OppositionFeatureStates[1].Id };
            public FingerFeatureConfig None =>
                new FingerFeatureConfig { Feature = FingerFeature.Opposition, Mode = _mode, State = FingerFeatureProperties.OppositionFeatureStates[2].Id };
        }
    }


    public class TransformFeatureConfigBuilder : FeatureConfigBuilder
    {
        public static BuildCondition<TrueFalseStateBuilder> WristUp { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.WristUp));

        public static BuildCondition<TrueFalseStateBuilder> WristDown { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.WristDown));

        public static BuildCondition<TrueFalseStateBuilder> PalmDown { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.PalmDown));

        public static BuildCondition<TrueFalseStateBuilder> PalmUp { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.PalmUp));

        public static BuildCondition<TrueFalseStateBuilder> PalmTowardsFace { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.PalmTowardsFace));

        public static BuildCondition<TrueFalseStateBuilder> PalmAwayFromFace { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.PalmAwayFromFace));

        public static BuildCondition<TrueFalseStateBuilder> FingersUp { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.FingersUp));

        public static BuildCondition<TrueFalseStateBuilder> FingersDown { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.FingersDown));

        public static BuildCondition<TrueFalseStateBuilder> PinchClear { get; } =
            new BuildCondition<TrueFalseStateBuilder>(mode => new TrueFalseStateBuilder(mode, TransformFeature.PinchClear));

        public class TrueFalseStateBuilder
        {
            private readonly FeatureStateActiveMode _mode;
            private readonly TransformFeature _transformFeature;
            private readonly FeatureStateDescription[] _states;

            public TrueFalseStateBuilder(FeatureStateActiveMode featureStateActiveMode,
                TransformFeature transformFeature)
            {
                _mode = featureStateActiveMode;
                _transformFeature = transformFeature;
                _states = TransformFeatureProperties.FeatureDescriptions[_transformFeature].FeatureStates;
            }

            public TransformFeatureConfig Open =>
                new TransformFeatureConfig { Feature = _transformFeature, Mode = _mode, State = _states[0].Id };
            public TransformFeatureConfig Closed =>
                new TransformFeatureConfig { Feature = _transformFeature, Mode = _mode, State = _states[1].Id };
        }
    }
}
