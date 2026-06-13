using System;
using System.Collections.Generic;
using System.Numerics;

namespace CS2_ESP
{
    public class Entity
    {
        public IntPtr PawnAddress { get; set; }
        public IntPtr ControllerAddress { get; set; }
        public int Team { get; set; }
        public int Health { get; set; }
        public Vector3 Position3D { get; set; }
        public Vector3 ViewOffset { get; set; }
        public Vector2 Position2D { get; set; }
        public Vector2 ViewPosition2D { get; set; }
        public List<Vector3> Bones3D { get; set; } = new List<Vector3>();
        public List<Vector2> Bones2D { get; set; } = new List<Vector2>();
    }

    public enum BoneIds
    {
        Head = 6,
        Neck = 5,
        Spine = 4,
        Spine1 = 2,
        LeftShoulder = 8,
        LeftArm = 9,
        LeftHand = 11,
        RightShoulder = 13,
        RightArm = 14,
        RightHand = 16,
        LeftHip = 22,
        LeftKnee = 23,
        LeftFoot = 24,
        RightHip = 25,
        RightKnee = 26,
        RightFoot = 27
    }
}
