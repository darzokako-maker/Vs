using System;
using System.Collections.Generic;
using System.Numerics;
using Swed64;

namespace CS2_ESP
{
    public static class Calculate
    {
        public static Vector2 WorldToScreen(float[] matrix, Vector3 pos, Vector2 windowSize)
        {
            if (matrix == null || matrix.Length < 16) return new Vector2(-99, -99);

            float screenW = (matrix[12] * pos.X) + (matrix[13] * pos.Y) + (matrix[14] * pos.Z) + matrix[15];

            if (screenW > 0.001f)
            {
                float screenX = (matrix[0] * pos.X) + (matrix[1] * pos.Y) + (matrix[2] * pos.Z) + matrix[3];
                float screenY = (matrix[4] * pos.X) + (matrix[5] * pos.Y) + (matrix[6] * pos.Z) + matrix[7];

                float x = (windowSize.X / 2) + (windowSize.X / 2) * screenX / screenW;
                float y = (windowSize.Y / 2) - (windowSize.Y / 2) * screenY / screenW;

                return new Vector2(x, y);
            }
            return new Vector2(-99, -99);
        }

        public static List<Vector3> ReadBones(IntPtr boneMatrix, Swed swed)
        {
            List<Vector3> bones = new List<Vector3>();
            if (boneMatrix == IntPtr.Zero || swed == null) return bones;

            try
            {
                foreach (BoneIds boneId in Enum.GetValues(typeof(BoneIds)))
                {
                    float x = swed.ReadFloat(boneMatrix + ((int)boneId * 32) + 0);
                    float y = swed.ReadFloat(boneMatrix + ((int)boneId * 32) + 4);
                    float z = swed.ReadFloat(boneMatrix + ((int)boneId * 32) + 8);

                    bones.Add(new Vector3(x, y, z));
                }
            }
            catch
            {
                bones.Clear();
            }

            return bones;
        }

        public static List<Vector2> ReadBones2D(List<Vector3> bones3D, float[] viewMatrix, Vector2 screenSize)
        {
            List<Vector2> bones2D = new List<Vector2>();
            if (bones3D == null || bones3D.Count == 0 || viewMatrix == null) return bones2D;

            foreach (var bone in bones3D)
            {
                bones2D.Add(WorldToScreen(viewMatrix, bone, screenSize));
            }

            return bones2D;
        }
    }
}
