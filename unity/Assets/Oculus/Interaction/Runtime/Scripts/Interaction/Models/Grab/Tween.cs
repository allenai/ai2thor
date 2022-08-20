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
using UnityEngine;

namespace Oculus.Interaction
{
    public class Tween : IMovement
    {
        private List<TweenCurve> _tweenCurves;
        private Pose _pose;
        public Pose Pose => _pose;

        private Pose _startPose;
        public Pose StartPose => _startPose;

        private float _maxOverlapTime;
        private float _tweenTime;
        private AnimationCurve _animationCurve;

        public bool Stopped => _tweenCurves.TrueForAll(t => t.PrevProgress >= 1f);

        private class TweenCurve
        {
            public ProgressCurve Curve;
            public float PrevProgress;
            public Pose Current;
            public Pose Target;
        }

        public Tween(Pose start, float tweenTime = 0.5f, float maxOverlapTime = 0.25f, AnimationCurve curve = null)
        {
            _pose = _startPose = start;
            _tweenTime = tweenTime;
            _maxOverlapTime = maxOverlapTime;

            _tweenCurves = new List<TweenCurve>();
            _animationCurve = curve ?? AnimationCurve.EaseInOut(0, 0, 1, 1);
            TweenToInTime(_pose, 0);
        }

        private void TweenToInTime(Pose target, float time)
        {
            Pose start = _pose;
            if (_tweenCurves.Count > 0)
            {
                TweenCurve previousCurve = _tweenCurves[_tweenCurves.Count - 1];
                float progressIn = previousCurve.Curve.ProgressIn(Mathf.Min(_maxOverlapTime, time));
                if (progressIn != 1.0f)
                {
                    float deltaEase = progressIn - previousCurve.PrevProgress;
                    float remainEase = 1.0f - previousCurve.PrevProgress;
                    float percentTravel = deltaEase / remainEase;
                    start = previousCurve.Current;
                    start.Lerp(in previousCurve.Target, percentTravel);
                }
            }

            TweenCurve tweenCurve = new TweenCurve()
            {
                Curve = new ProgressCurve(_animationCurve, time),
                PrevProgress = 0f,
                Current = start,
                Target = target
            };

            _tweenCurves.Add(tweenCurve);
            tweenCurve.Curve.Start();
        }

        public void MoveTo(Pose target)
        {
            if (_pose.Equals(target))
            {
                StopAndSetPose(target);
                return;
            }

            TweenToInTime(target, _tweenTime);
        }

        public void UpdateTarget(Pose target)
        {
            _tweenCurves[_tweenCurves.Count - 1].Target = target;
        }

        public void StopAndSetPose(Pose source)
        {
            _tweenCurves.Clear();
            _pose = source;
            TweenToInTime(source, 0);
        }

        public void Tick()
        {
            for (int i = _tweenCurves.Count - 1; i >= 0; i--)
            {
                TweenCurve tweenCurve = _tweenCurves[i];
                float progress = tweenCurve.Curve.Progress();
                if (progress == 1.0f)
                {
                    tweenCurve.Current = tweenCurve.Target;
                    tweenCurve.PrevProgress = 1.0f;
                    continue;
                }

                float deltaEase = progress - tweenCurve.PrevProgress;
                float remainEase = 1.0f - tweenCurve.PrevProgress;
                float percentTravel = deltaEase / remainEase;

                tweenCurve.Current.Lerp(in tweenCurve.Target, percentTravel);
                tweenCurve.PrevProgress = progress;
            }

            float multiplier = 1.0f;
            float overlap = 0.0f;
            Pose pose = _tweenCurves[_tweenCurves.Count - 1].Current;
            for (int i = _tweenCurves.Count - 2; i >= 0; i--)
            {
                TweenCurve nextCurve = _tweenCurves[i + 1];
                float timeProgress = nextCurve.Curve.ProgressTime();
                if(nextCurve.Curve.AnimationLength == 0f)
                {
                    overlap = 1.0f;
                }
                else
                {
                    overlap = Mathf.Min(_maxOverlapTime, timeProgress) /
                        Mathf.Min(_maxOverlapTime, nextCurve.Curve.AnimationLength);
                }

                if (overlap == 1.0f)
                {
                    _tweenCurves.RemoveRange(0, i);
                    break;
                }
                multiplier = (1 - overlap) * multiplier;
                Pose easeCurve = _tweenCurves[i].Current;
                pose.Lerp(in easeCurve, multiplier);
            }

            _pose = pose;
        }
    }
}
