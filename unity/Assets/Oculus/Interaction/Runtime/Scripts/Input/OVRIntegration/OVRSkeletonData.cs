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

using UnityEngine;

namespace Oculus.Interaction.Input
{
    public class OVRSkeletonData
    {
#if UNITY_EDITOR
        // This method can be used to generate a serialized skeleton in C# format.
        public static string CreateString(OVRPlugin.Skeleton2 skeleton)
        {
            string bonesJson = "";
            for (int i = 0; i < skeleton.NumBones; i++)
            {
                var bone = skeleton.Bones[i];
                bonesJson +=
                    "new OVRPlugin.Bone() { " +
                    "Id=OVRPlugin.BoneId." + bone.Id.ToString() + ", " +
                    "ParentBoneIndex=" + bone.ParentBoneIndex + ", " +
                    "Pose=new OVRPlugin.Posef() { " +
                        "Position=new OVRPlugin.Vector3f() {x=" + bone.Pose.Position.x + "f,y=" + bone.Pose.Position.y + "f,z=" + bone.Pose.Position.z + "f}, " +
                        "Orientation=new OVRPlugin.Quatf(){x=" + bone.Pose.Orientation.x + "f,y=" + bone.Pose.Orientation.y + "f,z=" + bone.Pose.Orientation.z + "f,w=" + bone.Pose.Orientation.w + "f}}}";
                if (i != skeleton.Bones.Length - 1)
                {
                    bonesJson += ",";
                }
                bonesJson += "\n";
            }

            string boneCapsulesJson = "";
            for (int i = 0; i < skeleton.NumBoneCapsules; i++)
            {
                var cap = skeleton.BoneCapsules[i];
                boneCapsulesJson += "new OVRPlugin.BoneCapsule() { BoneIndex=" + cap.BoneIndex + ", Radius=" + cap.Radius + "f, " +
                    "StartPoint=new OVRPlugin.Vector3f() {x=" + cap.StartPoint.x + "f,y=" + cap.StartPoint.y + "f,z=" + cap.StartPoint.z + "f}, " +
                    "EndPoint=new OVRPlugin.Vector3f() {x=" + cap.EndPoint.x + "f,y=" + cap.EndPoint.y + "f,z=" + cap.EndPoint.z + "f}}";
                if (i != skeleton.Bones.Length - 1)
                {
                    boneCapsulesJson += ",";
                }
                boneCapsulesJson += "\n";
            }

            return "new OVRPlugin.Skeleton2() { " +
                "Type=OVRPlugin.SkeletonType." + skeleton.Type.ToString() + ", " +
                "NumBones=" + skeleton.NumBones + ", " +
                "NumBoneCapsules=" + skeleton.NumBoneCapsules + ", " +
                "Bones=new OVRPlugin.Bone[] {" + bonesJson + "}, " +
                "BoneCapsules=new OVRPlugin.BoneCapsule[] {" + boneCapsulesJson + "}" +
                "};";
        }
#endif

        public static readonly OVRPlugin.Skeleton2 LeftSkeleton = new OVRPlugin.Skeleton2()
        {
            Type = OVRPlugin.SkeletonType.HandLeft,
            NumBones = 24,
            NumBoneCapsules = 19,
            Bones = new OVRPlugin.Bone[] {
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Start, ParentBoneIndex=-1, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0f,y=0f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_ForearmStub, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0f,y=0f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb0, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.0200693f,y=0.0115541f,z=-0.01049652f}, Orientation=new OVRPlugin.Quatf(){x=0.3753869f,y=0.4245841f,z=-0.007778856f,w=0.8238644f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb1, ParentBoneIndex=2, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02485256f,y=-9.31E-10f,z=-1.863E-09f}, Orientation=new OVRPlugin.Quatf(){x=0.2602303f,y=0.02433088f,z=0.125678f,w=0.9570231f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb2, ParentBoneIndex=3, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.03251291f,y=5.82E-10f,z=1.863E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.08270377f,y=-0.0769617f,z=-0.08406223f,w=0.9900357f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb3, ParentBoneIndex=4, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.0337931f,y=3.26E-09f,z=1.863E-09f}, Orientation=new OVRPlugin.Quatf(){x=0.08350593f,y=0.06501573f,z=-0.05827406f,w=0.9926752f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.09599624f,y=0.007316455f,z=-0.02355068f}, Orientation=new OVRPlugin.Quatf(){x=0.03068309f,y=-0.01885559f,z=0.04328144f,w=0.9984136f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index2, ParentBoneIndex=6, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.0379273f,y=-5.82E-10f,z=-5.97E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.02585241f,y=-0.007116061f,z=0.003292944f,w=0.999635f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index3, ParentBoneIndex=7, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02430365f,y=-6.73E-10f,z=-6.75E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.016056f,y=-0.02714872f,z=-0.072034f,w=0.9969034f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.09564661f,y=0.002543155f,z=-0.001725906f}, Orientation=new OVRPlugin.Quatf(){x=-0.009066326f,y=-0.05146559f,z=0.05183575f,w=0.9972874f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle2, ParentBoneIndex=9, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.042927f,y=-8.51E-10f,z=-1.193E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.01122823f,y=-0.004378874f,z=-0.001978267f,w=0.9999254f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle3, ParentBoneIndex=10, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02754958f,y=3.09E-10f,z=1.128E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.03431955f,y=-0.004611839f,z=-0.09300701f,w=0.9950631f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.0886938f,y=0.006529308f,z=0.01746524f}, Orientation=new OVRPlugin.Quatf(){x=-0.05315936f,y=-0.1231034f,z=0.04981349f,w=0.9897162f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring2, ParentBoneIndex=12, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.0389961f,y=0f,z=5.24E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.03363252f,y=-0.00278984f,z=0.00567602f,w=0.9994143f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring3, ParentBoneIndex=13, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02657339f,y=1.281E-09f,z=1.63E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.003477462f,y=0.02917945f,z=-0.02502854f,w=0.9992548f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky0, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.03407356f,y=0.009419836f,z=0.02299858f}, Orientation=new OVRPlugin.Quatf(){x=-0.207036f,y=-0.1403428f,z=0.0183118f,w=0.9680417f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky1, ParentBoneIndex=15, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.04565055f,y=9.97679E-07f,z=-2.193963E-06f}, Orientation=new OVRPlugin.Quatf(){x=0.09111304f,y=0.00407137f,z=0.02812923f,w=0.9954349f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky2, ParentBoneIndex=16, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.03072042f,y=1.048E-09f,z=-1.75E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.03761665f,y=-0.04293772f,z=-0.01328605f,w=0.9982809f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky3, ParentBoneIndex=17, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02031138f,y=-2.91E-10f,z=9.31E-10f}, Orientation=new OVRPlugin.Quatf(){x=0.0006447434f,y=0.04917067f,z=-0.02401883f,w=0.9985014f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_ThumbTip, ParentBoneIndex=5, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02459077f,y=-0.001026974f,z=0.0006703701f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_IndexTip, ParentBoneIndex=8, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02236338f,y=-0.00102507f,z=0.0002956076f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_MiddleTip, ParentBoneIndex=11, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02496492f,y=-0.001137299f,z=0.0003086528f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_RingTip, ParentBoneIndex=14, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02432613f,y=-0.001608172f,z=0.000257905f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_PinkyTip, ParentBoneIndex=18, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0.02192238f,y=-0.001216086f,z=-0.0002464796f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
        },
            BoneCapsules = new OVRPlugin.BoneCapsule[] {
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.01822828f, StartPoint=new OVRPlugin.Vector3f() {x=0.02755879f,y=0.01404149f,z=-0.01685145f}, EndPoint=new OVRPlugin.Vector3f() {x=0.07794081f,y=0.009090679f,z=-0.02178327f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.02323196f, StartPoint=new OVRPlugin.Vector3f() {x=0.02632602f,y=0.008661013f,z=-0.006531342f}, EndPoint=new OVRPlugin.Vector3f() {x=0.07255958f,y=0.004580691f,z=-0.003326343f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.01608828f, StartPoint=new OVRPlugin.Vector3f() {x=0.0297035f,y=0.00920606f,z=0.01111641f}, EndPoint=new OVRPlugin.Vector3f() {x=0.07271415f,y=0.007254403f,z=0.01574543f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.02346085f, StartPoint=new OVRPlugin.Vector3f() {x=0.02844799f,y=0.008827154f,z=0.01446979f}, EndPoint=new OVRPlugin.Vector3f() {x=0.06036391f,y=0.009573798f,z=0.02133043f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=3, Radius=0.01838252f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=2.561E-09f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.03251291f,y=6.98E-10f,z=-3.492E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=4, Radius=0.01028295f, StartPoint=new OVRPlugin.Vector3f() {x=7.451E-09f,y=2.794E-09f,z=-3.725E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.03379309f,y=6.519E-09f,z=-8.382E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=5, Radius=0.009768805f, StartPoint=new OVRPlugin.Vector3f() {x=7.451E-09f,y=5.588E-09f,z=-4.657E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.01500075f,y=-0.0006525163f,z=0.0005929575f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=6, Radius=0.01029526f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=0f,z=-1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.03792731f,y=4.66E-10f,z=-3.725E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=7, Radius=0.008038102f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=-9.31E-10f,z=-1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.02430364f,y=-1.863E-09f,z=-3.725E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=8, Radius=0.007636196f, StartPoint=new OVRPlugin.Vector3f() {x=-1.4901E-08f,y=-1.863E-09f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=0.01507758f,y=-0.0005028695f,z=6.049499E-05f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=9, Radius=0.01117394f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=-4.66E-10f,z=9.31E-10f}, EndPoint=new OVRPlugin.Vector3f() {x=0.042927f,y=-2.328E-09f,z=-9.31E-10f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=10, Radius=0.008030958f, StartPoint=new OVRPlugin.Vector3f() {x=1.4901E-08f,y=-4.66E-10f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=0.02754962f,y=-4.66E-10f,z=-1.863E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=11, Radius=0.007629411f, StartPoint=new OVRPlugin.Vector3f() {x=1.4901E-08f,y=-3.725E-09f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=0.01719159f,y=-0.0007450115f,z=0.0004036371f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=12, Radius=0.009922137f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=2.33E-10f,z=2.328E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.03899612f,y=0f,z=4.66E-10f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=13, Radius=0.007611672f, StartPoint=new OVRPlugin.Vector3f() {x=1.4901E-08f,y=-4.66E-10f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.02657339f,y=1.397E-09f,z=0f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=14, Radius=0.007231089f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=9.31E-10f,z=2.328E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.01632451f,y=-0.001288094f,z=0.0001235888f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=16, Radius=0.008483353f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=-2.33E-10f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.03072041f,y=-1.164E-09f,z=0f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=17, Radius=0.006764194f, StartPoint=new OVRPlugin.Vector3f() {x=-7.451E-09f,y=-1.717E-09f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.02031137f,y=1.46E-10f,z=1.863E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=18, Radius=0.006425985f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=0f,z=-1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=0.01507002f,y=-0.0006056242f,z=-2.491474E-05f}},
        }
        };

        public static readonly OVRPlugin.Skeleton2 RightSkeleton = new OVRPlugin.Skeleton2()
        {
            Type = OVRPlugin.SkeletonType.HandRight,
            NumBones = 24,
            NumBoneCapsules = 19,
            Bones = new OVRPlugin.Bone[] {new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Start, ParentBoneIndex=-1, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0f,y=0f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_ForearmStub, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=0f,y=0f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb0, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.0200693f,y=-0.0115541f,z=0.01049652f}, Orientation=new OVRPlugin.Quatf(){x=0.3753869f,y=0.4245841f,z=-0.007778856f,w=0.8238644f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb1, ParentBoneIndex=2, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02485256f,y=2.328E-09f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=0.2602303f,y=0.02433088f,z=0.125678f,w=0.9570231f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb2, ParentBoneIndex=3, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.03251291f,y=-1.16E-10f,z=0f}, Orientation=new OVRPlugin.Quatf(){x=-0.08270377f,y=-0.0769617f,z=-0.08406223f,w=0.9900357f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Thumb3, ParentBoneIndex=4, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.0337931f,y=-3.26E-09f,z=-1.863E-09f}, Orientation=new OVRPlugin.Quatf(){x=0.08350593f,y=0.06501573f,z=-0.05827406f,w=0.9926752f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.09599624f,y=-0.007316455f,z=0.02355068f}, Orientation=new OVRPlugin.Quatf(){x=0.03068309f,y=-0.01885559f,z=0.04328144f,w=0.9984136f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index2, ParentBoneIndex=6, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.0379273f,y=1.16E-10f,z=5.97E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.02585241f,y=-0.007116061f,z=0.003292944f,w=0.999635f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Index3, ParentBoneIndex=7, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02430365f,y=6.73E-10f,z=6.75E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.016056f,y=-0.02714872f,z=-0.072034f,w=0.9969034f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.09564661f,y=-0.002543155f,z=0.001725906f}, Orientation=new OVRPlugin.Quatf(){x=-0.009066326f,y=-0.05146559f,z=0.05183575f,w=0.9972874f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle2, ParentBoneIndex=9, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.042927f,y=1.317E-09f,z=1.193E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.01122823f,y=-0.004378874f,z=-0.001978267f,w=0.9999254f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Middle3, ParentBoneIndex=10, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02754958f,y=-7.71E-10f,z=-1.12E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.03431955f,y=-0.004611839f,z=-0.09300701f,w=0.9950631f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring1, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.0886938f,y=-0.006529307f,z=-0.01746524f}, Orientation=new OVRPlugin.Quatf(){x=-0.05315936f,y=-0.1231034f,z=0.04981349f,w=0.9897162f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring2, ParentBoneIndex=12, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.0389961f,y=-4.66E-10f,z=-5.24E-10f}, Orientation=new OVRPlugin.Quatf(){x=-0.03363252f,y=-0.00278984f,z=0.00567602f,w=0.9994143f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Ring3, ParentBoneIndex=13, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02657339f,y=-1.281E-09f,z=-1.63E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.003477462f,y=0.02917945f,z=-0.02502854f,w=0.9992548f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky0, ParentBoneIndex=0, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.03407356f,y=-0.009419835f,z=-0.02299858f}, Orientation=new OVRPlugin.Quatf(){x=-0.207036f,y=-0.1403428f,z=0.0183118f,w=0.9680417f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky1, ParentBoneIndex=15, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.04565055f,y=-9.98611E-07f,z=2.193963E-06f}, Orientation=new OVRPlugin.Quatf(){x=0.09111304f,y=0.00407137f,z=0.02812923f,w=0.9954349f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky2, ParentBoneIndex=16, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.03072042f,y=6.98E-10f,z=1.106E-09f}, Orientation=new OVRPlugin.Quatf(){x=-0.03761665f,y=-0.04293772f,z=-0.01328605f,w=0.9982809f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_Pinky3, ParentBoneIndex=17, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02031138f,y=-1.455E-09f,z=-1.397E-09f}, Orientation=new OVRPlugin.Quatf(){x=0.0006447434f,y=0.04917067f,z=-0.02401883f,w=0.9985014f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_ThumbTip, ParentBoneIndex=5, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02459077f,y=0.001026974f,z=-0.0006703701f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_IndexTip, ParentBoneIndex=8, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02236338f,y=0.00102507f,z=-0.0002956076f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_MiddleTip, ParentBoneIndex=11, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02496492f,y=0.001137299f,z=-0.0003086528f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_RingTip, ParentBoneIndex=14, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02432613f,y=0.001608172f,z=-0.000257905f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
            new OVRPlugin.Bone() { Id=OVRPlugin.BoneId.Hand_PinkyTip, ParentBoneIndex=18, Pose=new OVRPlugin.Posef() { Position=new OVRPlugin.Vector3f() {x=-0.02192238f,y=0.001216086f,z=0.0002464796f}, Orientation=new OVRPlugin.Quatf(){x=0f,y=0f,z=0f,w=1f}}},
        },
            BoneCapsules = new OVRPlugin.BoneCapsule[] {new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.01822828f, StartPoint=new OVRPlugin.Vector3f() {x=-0.02755879f,y=-0.01404148f,z=0.01685145f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.07794081f,y=-0.009090678f,z=0.02178326f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.02323196f, StartPoint=new OVRPlugin.Vector3f() {x=-0.02632602f,y=-0.008661013f,z=0.006531343f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.07255958f,y=-0.004580691f,z=0.003326343f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.01608828f, StartPoint=new OVRPlugin.Vector3f() {x=-0.0297035f,y=-0.00920606f,z=-0.01111641f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.07271415f,y=-0.007254403f,z=-0.01574543f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=0, Radius=0.02346085f, StartPoint=new OVRPlugin.Vector3f() {x=-0.02844799f,y=-0.008827153f,z=-0.01446979f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.06036392f,y=-0.009573797f,z=-0.02133043f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=3, Radius=0.01838251f, StartPoint=new OVRPlugin.Vector3f() {x=3.725E-09f,y=-6.98E-10f,z=-2.794E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.03251291f,y=-6.98E-10f,z=2.561E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=4, Radius=0.01028296f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=-9.31E-10f,z=5.588E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.03379308f,y=-4.657E-09f,z=1.0245E-08f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=5, Radius=0.009768807f, StartPoint=new OVRPlugin.Vector3f() {x=-7.451E-09f,y=1.863E-09f,z=8.382E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.0150008f,y=0.0006525647f,z=-0.000592957f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=6, Radius=0.01029526f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=4.66E-10f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.03792731f,y=-4.66E-10f,z=3.725E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=7, Radius=0.008038101f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=9.31E-10f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.02430364f,y=1.863E-09f,z=3.725E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=8, Radius=0.007636196f, StartPoint=new OVRPlugin.Vector3f() {x=1.4901E-08f,y=1.863E-09f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.01507759f,y=0.0005028695f,z=-6.052852E-05f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=9, Radius=0.01117394f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=0f,z=-9.31E-10f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.042927f,y=1.863E-09f,z=9.31E-10f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=10, Radius=0.008030958f, StartPoint=new OVRPlugin.Vector3f() {x=-1.4901E-08f,y=0f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.02754962f,y=4.66E-10f,z=1.863E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=11, Radius=0.00762941f, StartPoint=new OVRPlugin.Vector3f() {x=-1.4901E-08f,y=1.863E-09f,z=0f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.01719156f,y=0.0007450022f,z=-0.0004036473f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=12, Radius=0.009922139f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=-2.33E-10f,z=-2.328E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.03899612f,y=4.66E-10f,z=-4.66E-10f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=13, Radius=0.007611674f, StartPoint=new OVRPlugin.Vector3f() {x=-1.4901E-08f,y=1.863E-09f,z=-1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.02657339f,y=0f,z=0f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=14, Radius=0.00723109f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=9.31E-10f,z=-2.328E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.01632455f,y=0.001288087f,z=-0.0001235851f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=16, Radius=0.008483353f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=2.33E-10f,z=-1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.03072041f,y=1.164E-09f,z=0f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=17, Radius=0.006764191f, StartPoint=new OVRPlugin.Vector3f() {x=7.451E-09f,y=1.717E-09f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.02031137f,y=-1.46E-10f,z=-1.863E-09f}},
            new OVRPlugin.BoneCapsule() { BoneIndex=18, Radius=0.006425982f, StartPoint=new OVRPlugin.Vector3f() {x=0f,y=0f,z=1.863E-09f}, EndPoint=new OVRPlugin.Vector3f() {x=-0.01507004f,y=0.0006056186f,z=2.490915E-05f}},
        }
        };
    }
}
