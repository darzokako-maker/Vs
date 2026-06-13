using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using ImGuiNET;
using ClickableTransparentOverlay;

namespace CS2_ESP
{
    public class Renderer : Overlay
    {
        public readonly object entityLock = new object();
        public List<Entity> entitiesToDraw = new List<Entity>();

        public Vector2 screenSize = new Vector2(1920, 1080);

        public bool showMenu = true;
        public bool enableBoxESP = true;
        public bool enableLineESP = false;
        public bool enableBoneESP = true;

        public Vector4 boxColor = new Vector4(1f, 0f, 0f, 1f);
        public Vector4 boneColor = new Vector4(0f, 1f, 0f, 1f);

        protected override Task Render()
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Insert))
            {
                showMenu = !showMenu;
            }

            if (showMenu)
            {
                ImGui.Begin("Swedz C# - CS2 External (2026)", ref showMenu);
                ImGui.Checkbox("Box ESP", ref enableBoxESP);
                ImGui.Checkbox("Line ESP", ref enableLineESP);
                ImGui.Checkbox("Bone ESP", ref enableBoneESP);
                ImGui.ColorEdit4("Box Color", ref boxColor);
                ImGui.ColorEdit4("Bone Color", ref boneColor);
                ImGui.End();
            }

            var drawList = ImGui.GetBackgroundDrawList();

            lock (entityLock)
            {
                foreach (var entity in entitiesToDraw)
                {
                    if (entity.Position2D.X == -99 || entity.Position2D.Y == -99)
                        continue;

                    if (enableBoxESP)
                    {
                        float height = entity.Position2D.Y - entity.ViewPosition2D.Y;
                        float width = height / 2.4f;

                        Vector2 topLeft = new Vector2(entity.Position2D.X - width / 2, entity.ViewPosition2D.Y);
                        Vector2 bottomRight = new Vector2(entity.Position2D.X + width / 2, entity.Position2D.Y);

                        uint colorUint = ImGui.ColorConvertFloat4ToU32(boxColor);
                        drawList.AddRect(topLeft, bottomRight, colorUint, 0.0f, ImDrawFlags.None, 2.0f);
                    }

                    if (enableLineESP)
                    {
                        uint colorUint = ImGui.ColorConvertFloat4ToU32(boxColor);
                        drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.Position2D, colorUint, 1.5f);
                    }

                    if (enableBoneESP && entity.Bones2D.Count > 0)
                    {
                        uint bColorUint = ImGui.ColorConvertFloat4ToU32(boneColor);

                        try
                        {
                            // Omurga
                            DrawBoneLine(drawList, entity.Bones2D, 0, 1, bColorUint); 
                            DrawBoneLine(drawList, entity.Bones2D, 1, 2, bColorUint); 
                            DrawBoneLine(drawList, entity.Bones2D, 2, 3, bColorUint); 

                            // Sol Kol
                            DrawBoneLine(drawList, entity.Bones2D, 1, 4, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 4, 5, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 5, 6, bColorUint);

                            // Sağ Kol
                            DrawBoneLine(drawList, entity.Bones2D, 1, 7, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 7, 8, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 8, 9, bColorUint);

                            // Sol Bacak
                            DrawBoneLine(drawList, entity.Bones2D, 3, 10, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 10, 11, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 11, 12, bColorUint);

                            // Sağ Bacak
                            DrawBoneLine(drawList, entity.Bones2D, 3, 13, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 13, 14, bColorUint);
                            DrawBoneLine(drawList, entity.Bones2D, 14, 15, bColorUint);
                        }
                        catch
                        {
                            // Sınır taşma hatalarını yutmak için
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void DrawBoneLine(ImDrawListPtr drawList, List<Vector2> bones2D, int fromIndex, int toIndex, uint color)
        {
            if (bones2D != null && fromIndex < bones2D.Count && toIndex < bones2D.Count)
            {
                if (bones2D[fromIndex].X != -99 && bones2D[toIndex].X != -99)
                {
                    drawList.AddLine(bones2D[fromIndex], bones2D[toIndex], color, 1.5f);
                }
            }
        }
    }
}
