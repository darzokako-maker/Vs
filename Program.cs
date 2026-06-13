using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Swed64;

namespace CS2_ESP
{
    class Program
    {
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static class Offsets
        {
            public static int dwEntityList = 0x24E76A0;
            public static int dwLocalPlayerPawn = 0x2341698;
            public static int dwViewMatrix = 0x2346B30;

            public static int m_iTeamNum = 0x3EB;          
            public static int m_iHealth = 0x34C;           
            public static int m_vOldOrigin = 0x1390;       
            public static int m_vecViewOffset = 0xE70;     
            public static int m_pGameSceneNode = 0x330;    
            public static int m_modelState = 0x150;        
        }

        static void Main(string[] args)
        {
            Swed swed;
            try
            {
                // Bellek yöneticisini hata denetimiyle başlatıyoruz
                swed = new Swed("cs2");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HATA] CS2 surecine baglanilamadi. Oyunun acik oldugundan emin olun: {ex.Message}");
                Console.ReadLine();
                return;
            }

            IntPtr client = swed.GetModuleBase("client.dll");
            if (client == IntPtr.Zero)
            {
                Console.WriteLine("[HATA] client.dll modulu bellekte bulunamadi.");
                Console.ReadLine();
                return;
            }

            Renderer renderer = new Renderer();
            Thread renderThread = new Thread(() => renderer.Start().Wait());
            renderThread.Start();

            List<Entity> tempEntities = new List<Entity>();
            Console.WriteLine("ESP Modulu Yuklendi. Moduller dogrulandi.");

            while (true)
            {
                try
                {
                    // Çözünürlük güncellemesi
                    IntPtr hwnd = FindWindow("SDL_app", "Counter-Strike 2");
                    if (hwnd != IntPtr.Zero && GetClientRect(hwnd, out RECT rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        if (width > 0 && height > 0)
                        {
                            renderer.screenSize = new Vector2(width, height);
                        }
                    }

                    tempEntities.Clear();

                    float[] viewMatrix = swed.ReadMatrix(client + Offsets.dwViewMatrix);
                    if (viewMatrix == null || viewMatrix.Length < 16) continue;

                    IntPtr localPlayerPawn = swed.ReadPointer(client + Offsets.dwLocalPlayerPawn);
                    if (localPlayerPawn == IntPtr.Zero) continue;
                    
                    int localTeam = swed.ReadInt(localPlayerPawn + Offsets.m_iTeamNum);

                    IntPtr entityList = swed.ReadPointer(client + Offsets.dwEntityList);
                    if (entityList == IntPtr.Zero) continue;

                    IntPtr listEntry = swed.ReadPointer(entityList + 0x10);
                    if (listEntry == IntPtr.Zero) continue;

                    for (int i = 0; i < 64; i++)
                    {
                        IntPtr controller = swed.ReadPointer(listEntry + (i * 0x78));
                        if (controller == IntPtr.Zero) continue;

                        int pawnHandle = swed.ReadInt(controller + 0x7FC); // m_hPawn
                        if (pawnHandle == 0) continue;

                        IntPtr listEntry2 = swed.ReadPointer(entityList + (8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10));
                        if (listEntry2 == IntPtr.Zero) continue;

                        IntPtr pawn = swed.ReadPointer(listEntry2 + (120 * (pawnHandle & 0x1FF)));
                        if (pawn == IntPtr.Zero || pawn == localPlayerPawn) continue;

                        int health = swed.ReadInt(pawn + Offsets.m_iHealth);
                        if (health <= 0 || health > 100) continue;

                        int team = swed.ReadInt(pawn + Offsets.m_iTeamNum);

                        Vector3 position3D = swed.ReadVec(pawn + Offsets.m_vOldOrigin);
                        Vector3 viewOffset = swed.ReadVec(pawn + Offsets.m_vecViewOffset);
                        Vector3 headPosition3D = position3D + viewOffset;

                        IntPtr sceneNode = swed.ReadPointer(pawn + Offsets.m_pGameSceneNode);
                        List<Vector3> bones3D = new List<Vector3>();
                        if (sceneNode != IntPtr.Zero)
                        {
                            IntPtr boneMatrix = swed.ReadPointer(sceneNode + Offsets.m_modelState + 0x80); 
                            if (boneMatrix != IntPtr.Zero)
                            {
                                bones3D = Calculate.ReadBones(boneMatrix, swed);
                            }
                        }

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
                }
                catch (Exception ex)
                {
                    // Döngü içi kararsızlıklarda uygulamanın çökmesini engeller
                    Console.WriteLine($"[UYARI] Bellek okuma hatası atlandı: {ex.Message}");
                }

                Thread.Sleep(5);
            }
        }
    }
}
