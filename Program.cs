using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using Swed64;

namespace CS2_ESP
{
    class Program
    {
        // 11 Haziran 2026 tarihli dumper dosyalarınızdan alınan güncel offsetler ve şemalar:
        public static class Offsets
        {
            // offsets.hpp -> client_dll
            public static int dwEntityList = 0x24E76A0;
            public static int dwLocalPlayerPawn = 0x2341698;
            public static int dwViewMatrix = 0x2346B30;

            // client_dll.hpp -> C_BaseEntity / C_BasePlayerPawn / C_BaseModelEntity / CSkeletonInstance
            public static int m_iTeamNum = 0x3EB;          // C_BaseEntity -> m_iTeamNum (uint8)
            public static int m_iHealth = 0x34C;           // C_BaseEntity -> m_iHealth (int32)
            public static int m_vOldOrigin = 0x1390;       // C_BasePlayerPawn -> m_vOldOrigin (Vector)
            public static int m_vecViewOffset = 0xE70;     // C_BaseModelEntity -> m_vecViewOffset (CNetworkViewOffsetVector)
            public static int m_pGameSceneNode = 0x330;    // C_BaseEntity -> m_pGameSceneNode (CGameSceneNode*)
            public static int m_modelState = 0x150;        // CSkeletonInstance -> m_modelState (CModelState)
        }

        static void Main(string[] args)
        {
            Swed swed = new Swed("cs2");
            IntPtr client = swed.GetModuleBase("client.dll");

            Renderer renderer = new Renderer();
            Thread renderThread = new Thread(() => renderer.Start().Wait());
            renderThread.Start();

            List<Entity> tempEntities = new List<Entity>();

            Console.WriteLine("CS2 ESP Basariyla Baslatildi.");

            while (true)
            {
                tempEntities.Clear();

                float[] viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);

                IntPtr localPlayerPawn = swed.ReadPointer(client + Offsets.dwLocalPlayerPawn);
                if (localPlayerPawn == IntPtr.Zero) continue;
                
                int localTeam = swed.ReadInt(localPlayerPawn + Offsets.m_iTeamNum);

                IntPtr entityList = swed.ReadPointer(client + Offsets.dwEntityList);
                if (entityList == IntPtr.Zero) continue;

                IntPtr listEntry = swed.ReadPointer(entityList + 0x10);

                for (int i = 0; i < 64; i++)
                {
                    IntPtr controller = swed.ReadPointer(listEntry + (i * 0x78));
                    if (controller == IntPtr.Zero) continue;

                    int pawnHandle = swed.ReadInt(controller + 0x7FC); // m_hPawn
                    if (pawnHandle == 0) continue;

                    IntPtr listEntry2 = swed.ReadPointer(entityList + (8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10));
                    if (listEntry2 == IntPtr.Zero) continue;

                    IntPtr pawn = swed.ReadPointer(listEntry2 + (120 * (pawnHandle & 0x1FF)));
                    if (pawn == IntPtr.Zero) continue;
                    if (pawn == localPlayerPawn) continue;

                    int health = swed.ReadInt(pawn + Offsets.m_iHealth);
                    if (health <= 0 || health > 100) continue;

                    int team = swed.ReadInt(pawn + Offsets.m_iTeamNum);

                    Vector3 position3D = swed.ReadVec(pawn + Offsets.m_vOldOrigin);
                    Vector3 viewOffset = swed.ReadVec(pawn + Offsets.m_vecViewOffset);
                    Vector3 headPosition3D = position3D + viewOffset;

                    // Kemik (Bone) verilerini okuma
                    IntPtr sceneNode = swed.ReadPointer(pawn + Offsets.m_pGameSceneNode);
                    IntPtr boneMatrix = swed.ReadPointer(sceneNode + Offsets.m_modelState + 0x80); 

                    List<Vector3> bones3D = Calculate.ReadBones(boneMatrix, swed);

                    Entity entity = new Entity
                    {
                        PawnAddress = pawn,
                        ControllerAddress = controller,
                        Health = health,
                        Team = team,
                        Position3D = position3D,
                        ViewOffset = viewOffset,
                        Position2D = Calculate.WorldToScreen(viewMatrix, position3D, renderer.screenSize),
                        ViewPosition2D = Calculate.WorldToScreen(viewMatrix, headPosition3D, renderer.screenSize),
                        Bones3D = bones3D,
                        Bones2D = Calculate.ReadBones2D(bones3D, viewMatrix, renderer.screenSize)
                    };

                    tempEntities.Add(entity);
                }

                lock (renderer.entityLock)
                {
                    renderer.entitiesToDraw = new List<Entity>(tempEntities);
                }

                Thread.Sleep(5);
            }
        }
    }
}
