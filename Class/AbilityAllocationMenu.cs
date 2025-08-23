﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewModdingAPI;


namespace UnifiedExperienceSystem
{
    public class AbilityAllocationMenu : IClickableMenu
    {
        private readonly ModEntry mod;
        private readonly IMonitor log;
        private string? expandedRowKey;

        private IUnifiedExperienceAPI? uesApi;

        // UI config
        private const int yOffset = 60;
        private int RowHeight => mod.Config.SkillMenuRowSpacing;
        private int MaxVisibleRows => mod.Config.SkillMenuVisibleRows;

        // UI widgets
        private ClickableTextureComponent closeButton;
        private ClickableTextureComponent upArrow;
        private ClickableTextureComponent downArrow;
        
        //Hover Texts (Context Help)
        private string? _hoverText;
        private readonly List<(Rectangle bounds, string tooltip)> _hoverRegions = new();



        // Scroll & visuals
        private int scrollIndex = 0;
        private readonly Texture2D emojiTexture = Game1.content.Load<Texture2D>("LooseSprites/Emojis");

        // In-file view models
        private sealed class AbilityRowVM
        {
            public string ModId = "";
            public string ModName = "";
            public string AbilityId = "";
            public string AbilityName = "";
            public string Description = "";
            public long TotalExp = 0;
            public int Level = 0;
            public int MaxLevel = 0;
            public string? IconPath { get; set; }
        }

        private sealed class AbilityGroupVM
        {
            public string ModId = "";
            public string ModName = "";
            public List<AbilityRowVM> Abilities = new();
        }


        private struct Row
        {
            public bool IsHeader;
            public string HeaderText;
            public string ModId;
            public string AbilityId;
            public string AbilityName;
            public string Description;
            public int MaxLevel;
            public int Level;
            public long TotalExp;
            public bool AtMax;
            public int XpInto;
            public int XpNeeded;
            public int XpLevelTotal;


            public string? IconPath { get; set; }
            public Rectangle? IconSourceRect { get; set; }
        }

        private List<AbilityGroupVM> groups = new();

        public AbilityAllocationMenu(ModEntry mod)
            : base(
                Game1.uiViewport.Width / 2 - mod.Config.MenuWidth / 2,
                Game1.uiViewport.Height / 2 - mod.Config.MenuHeight / 2,
                mod.Config.MenuWidth,
                mod.Config.MenuHeight,
                true)
        {
            this.mod = mod;
            this.log = mod.Monitor;


            this.uesApi = mod.Helper.ModRegistry.GetApi<IUnifiedExperienceAPI>("Darkmushu.UnifiedExperienceSystem");

            RefreshData();

            closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width / 2 - 32, yPositionOnScreen + height - 80, 64, 64),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            int arrowSize = 64;
            int centerY = yPositionOnScreen + height / 2;
            int arrowX = xPositionOnScreen + width + 10;

            upArrow = new ClickableTextureComponent(
                new Rectangle(arrowX, centerY - arrowSize - 8, arrowSize, arrowSize),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                4f
            );
            downArrow = new ClickableTextureComponent(
                new Rectangle(arrowX, centerY + 8, arrowSize, arrowSize),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                4f
            );
        }


        private void RefreshData(bool preserveScroll = false)
        {
            int oldScroll = scrollIndex;

            groups = BuildAbilityListingForUi();

            if (preserveScroll)
            {
                // clamp to the new max (in case row count changed)
                var rows = BuildRowsForDraw();
                int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);
                scrollIndex = MathHelper.Clamp(oldScroll, 0, maxScroll);
            }
            else
            {
                scrollIndex = 0;
            }
        }


        private List<AbilityGroupVM> BuildAbilityListingForUi()
        {

            var infos = mod.GetAllAbilityInfos(uesApi) ?? new List<AbilityInfo>();

         
            var grouped = infos
                .GroupBy(i => i.ModId, StringComparer.OrdinalIgnoreCase)
                .Select(g => new AbilityGroupVM
                {
                    ModId = g.Key,
                    ModName = g.Key, 
                    Abilities = g
                        .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
                        .Select(a => new AbilityRowVM
                        {
                            ModId = a.ModId,
                            ModName = g.Key,
                            AbilityId = a.AbilityId,
                            AbilityName = a.DisplayName,
                            Description = a.Description,
                            TotalExp = (long)a.TotalExp,
                            Level = a.CurrentLevel,
                            MaxLevel = a.MaxLevel
                        })
                        .ToList()
                })
                .OrderBy(grp => grp.ModName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return grouped;
        }





        public override void draw(SpriteBatch b)
        {
            _hoverRegions.Clear();
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            // === Mini "icon menu" to the LEFT, aligned to Ability menu top ===
            const int MiniW = 48;
            const int MiniH = 48;
            const int MiniGap = -20;   // 0 = touching; negative overlaps slightly
            const int MiniYOffset = 85;

            int miniX = xPositionOnScreen - (MiniW + MiniGap);
            int miniY = yPositionOnScreen + MiniYOffset;

            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                miniX, miniY, MiniW, MiniH,
                Color.White,
                1f,
                drawShadow: false
            );

            var cursorsTex = Game1.content.Load<Texture2D>("LooseSprites/Cursors");
            Rectangle miniSrc = new Rectangle(391, 360, 11, 12);
            int pad = 8;
            float miniScale = Math.Min((MiniW - pad * 2f) / miniSrc.Width, (MiniH - pad * 2f) / miniSrc.Height);
            int miniIconW = (int)Math.Round(miniSrc.Width * miniScale);
            int miniIconH = (int)Math.Round(miniSrc.Height * miniScale);
            int miniIconX = miniX + (MiniW - miniIconW) / 2;
            int miniIconY = miniY + (MiniH - miniIconH) / 2;
            b.Draw(cursorsTex, new Rectangle(miniIconX, miniIconY, miniIconW, miniIconH), miniSrc, Color.White);
            // === End mini icon menu ===

            int titleY = yPositionOnScreen + 40 + yOffset;
            SpriteText.drawString(
                b,
                $"{mod.Helper.Translation.Get("config.toggleMenuHotkeysAbility.name")} - {mod.Helper.Translation.Get("ui.availablePoints")}: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );

            var rows = BuildRowsForDraw();
            int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            string? expandedTooltipToDraw = null;

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(RowHeight - 10, 48);

            // --- layout constants for per-row icon/text ---
            const int ContentPadLeft = 40;   // inner padding from dialog edge
            const int IconTextGap = 10;   // space between icon and text
            const int RowVPad = 4;    // top/bottom padding inside each row
            const int BaselineOffset = -16;  // tweak to visually match SpriteText baseline

            for (int i = 0; i < MaxVisibleRows && i + scrollIndex < rows.Count; i++)
            {
                var row = rows[i + scrollIndex];
                int y = rowStartY + i * RowHeight;

                if (row.IsHeader)
                {
                    SpriteText.drawString(b, row.HeaderText, xPositionOnScreen + 40, y, color: Color.DarkBlue);
                    continue;
                }

                // 1) Resolve row icon (from row.IconPath with fallback to magnifying glass)
                Texture2D rowIconTex;
                Rectangle rowIconSrc;
                if (!string.IsNullOrWhiteSpace(row.IconPath))
                {
                    try
                    {
                        rowIconTex = Game1.content.Load<Texture2D>(row.IconPath);
                        rowIconSrc = new Rectangle(0, 0, rowIconTex.Width, rowIconTex.Height);
                    }
                    catch
                    {
                        // fallback on load failure
                        rowIconTex = cursorsTex;
                        rowIconSrc = new Rectangle(322, 498, 12, 12);
                    }
                }
                else
                {
                    rowIconTex = cursorsTex;
                    rowIconSrc = new Rectangle(322, 498, 12, 12);
                }

                // 2) Scale and position the icon
                int iconMaxHRow = Math.Max(12, RowHeight - RowVPad * 2);
                float iconScaleRow = iconMaxHRow / (float)rowIconSrc.Height;
                int rowIconW = (int)Math.Round(rowIconSrc.Width * iconScaleRow);
                int rowIconH = (int)Math.Round(rowIconSrc.Height * iconScaleRow);
                int rowIconX = xPositionOnScreen + ContentPadLeft;
                int rowIconY = y + (RowHeight - rowIconH) / 2 + BaselineOffset;

                b.Draw(rowIconTex, new Rectangle(rowIconX, rowIconY, rowIconW, rowIconH), rowIconSrc, Color.White);

                // 3) Build the label text and draw it AFTER the icon
                var name = row.AbilityName ?? "";
                var shortName = name.Length > 12 ? (name.Substring(0, 12) + "..") : name;
                if (shortName.Length < 12)
                {
                    int spacesToAdd = 12 - shortName.Length;
                    if (spacesToAdd > 0) shortName = shortName + new string(' ', spacesToAdd);
                }

                bool showPlus = !row.AtMax;
                string text = row.AtMax
                    ? $"{shortName} (Lv{row.Level})"
                    : $"{shortName} (Lv{row.Level})   Need {row.XpNeeded} XP";

                // left edge for text is after the icon
                int textX = rowIconX + rowIconW + IconTextGap;

                // (click region for context help uses textX so it includes the icon offset)
                var textRect = new Rectangle(textX, y, width - 200, RowHeight);

                SpriteText.drawString(b, text, textX, y);

                string key = $"{row.ModId}/{row.AbilityId}";
                if (expandedRowKey == key)
                    expandedTooltipToDraw = BuildAbilityTooltip(row);

                // 4) Progress bar & [+] button (use textX for leftTextX so the bar doesn't overlap the icon)
                if (showPlus)
                {
                    int barHeight = 50;
                    int desiredBarWidth = 300;
                    int maxBarWidth = 300;
                    int rightEdgeOffset = 50;
                    int gapFromButton = 8;

                    int barOffsetY = -15;
                    float barAlpha = 0.5f;
                    float bgAlpha = 0.2f;
                    float borderAlpha = 0.4f;

                    int plusBtnSize = Math.Min(RowHeight - 10, 48);
                    Rectangle plusBtn = new Rectangle(
                        xPositionOnScreen + width - plusBtnSize - rightEdgeOffset,
                        y - 6,
                        plusBtnSize,
                        plusBtnSize
                    );

                    int barRight = plusBtn.Left - gapFromButton;
                    int leftTextX = textX; // honor icon
                    int maxSpace = Math.Max(0, barRight - leftTextX);
                    int barWidth = Math.Min(Math.Max(desiredBarWidth, 0), Math.Min(maxBarWidth, maxSpace));
                    if (barWidth < 60) barWidth = Math.Min(60, maxSpace);

                    int barY = y + (RowHeight - barHeight) / 2 + barOffsetY;
                    barY = Math.Max(y, Math.Min(barY, y + RowHeight - barHeight));

                    const int ExtendLeft = 100;
                    var barRect = new Rectangle(barRight - barWidth - ExtendLeft, barY, barWidth+ ExtendLeft, barHeight); 
                    b.Draw(Game1.staminaRect, barRect, Color.Black * bgAlpha);

                    if (row.AtMax)
                    {
                        b.Draw(Game1.staminaRect, barRect, Color.Gold * barAlpha);
                    }
                    else if (row.XpLevelTotal > 0)
                    {
                        int fill = (int)(barRect.Width * (row.XpInto / (float)row.XpLevelTotal));
                        if (fill > 0)
                            b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y, fill, barRect.Height), Color.Lime * barAlpha);
                    }

                    Color bc = Color.Black * borderAlpha;
                    b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y, barRect.Width, 1), bc);
                    b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y + barRect.Height - 1, barRect.Width, 1), bc);
                    b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y, 1, barRect.Height), bc);
                    b.Draw(Game1.staminaRect, new Rectangle(barRect.Right - 1, barRect.Y, 1, barRect.Height), bc);

                    // draw [+] button (unchanged)
                    Rectangle btn = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y - 6, buttonSize, buttonSize);
                    b.Draw(emojiTexture, btn, new Rectangle(108, 81, 9, 9), Color.White);
                }
            }

            upArrow.draw(b);
            downArrow.draw(b);
            closeButton.draw(b);
            if (!string.IsNullOrEmpty(expandedTooltipToDraw))
                IClickableMenu.drawHoverText(b, expandedTooltipToDraw, Game1.smallFont);
            drawMouse(b);
        }



        private string BuildAbilityTooltip(Row row)
        {
            var desc = string.IsNullOrEmpty(row.Description) ? "" : $"\n{row.Description}";
            return Game1.parseText(
                $"Mod: {row.ModId}\n" +
                $"Ability Name: {row.AbilityName}\n" +
                $"Level: {row.Level}\n" +
                $"MaxLevel: {row.MaxLevel}\n" +
                $"Total XP: {row.TotalExp}\n" +
                $"Description: {desc}",
                Game1.smallFont,
                Math.Max(400, width - 200)
            );
        }

        private string BuildPlusTooltip(Row row)
        {
            bool outOfPoints = mod.SaveData.UnspentSkillPoints <= 0;
            bool atMax = uesApi?.IsAbilityAtMax(row.ModId, row.AbilityId) ?? false;

            if (atMax) return "At max level";
            if (outOfPoints) return "No unspent points";

            var (into, needed, cap) = uesApi?.GetAbilityProgress(row.ModId, row.AbilityId) ?? (0, 0, 1);
            return $"Spend 1 point (+{ModEntry.EXP_PER_POINT} XP)\nNext level needs: {needed} XP";
        }

        public override void performHoverAction(int x, int y)
        {
            _hoverText = null; 
            base.performHoverAction(x, y);
        }


        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // --- mini icon bounds (must match draw) ---
            const int MiniW = 48;
            const int MiniH = 48;
            const int MiniGap = -20;
            const int MiniYOffset = 85;

            int miniX = xPositionOnScreen - (MiniW + MiniGap);
            int miniY = yPositionOnScreen + MiniYOffset;
            Rectangle miniIconBounds = new Rectangle(miniX, miniY, MiniW, MiniH);

            // click on mini icon -> go to Skill Allocation menu
            if (miniIconBounds.Contains(x, y))
            {
                if (playSound) Game1.playSound("smallSelect");
                Game1.activeClickableMenu = new SkillAllocationMenu(mod);
                Game1.playSound("bigSelect");
                return;
            }
            // --- end mini icon handling ---

            var rows = BuildRowsForDraw();
            int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            // --- navigation buttons ---
            if (upArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Max(0, scrollIndex - 1);
                Game1.playSound("shwip");
                return;
            }
            if (downArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Min(maxScroll, scrollIndex + 1);
                Game1.playSound("shwip");
                return;
            }
            if (closeButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
                return;
            }

            // --- rows (click-to-toggle context help, then [+] button) ---
            int titleY = yPositionOnScreen + 40 + yOffset;
            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(RowHeight - 10, 48);

            for (int i = 0; i < MaxVisibleRows && i + scrollIndex < rows.Count; i++)
            {
                var row = rows[i + scrollIndex];
                if (row.IsHeader)
                    continue;

                int rowY = rowStartY + i * RowHeight;

                // ContextHelp click to trigger
                var textRect = new Rectangle(xPositionOnScreen + 70, rowY, width - 200, RowHeight);
                if (textRect.Contains(x, y))
                {
                    string key = $"{row.ModId}/{row.AbilityId}";
                    expandedRowKey = (expandedRowKey == key) ? null : key;
                    Game1.playSound("smallSelect");
                    return;
                }

                // [+] button
                bool showPlus = !row.AtMax;
                if (!showPlus)
                    continue;

                Rectangle btn = new Rectangle(
                    xPositionOnScreen + width - buttonSize - 50,
                    rowY - 6,
                    buttonSize,
                    buttonSize
                );

                if (btn.Contains(x, y))
                {
                    if (mod.SaveData.UnspentSkillPoints > 0)
                    {
                        mod.AllocateAbilityPoints(row.ModId, row.AbilityId);

                        Game1.playSound("coin");
                        if (mod.Config.DebugMode)
                            log.Log($"[AbilityMenu] Allocated point to {row.ModId}/{row.AbilityId}. Remaining points: {mod.SaveData.UnspentSkillPoints}", LogLevel.Debug);

                        RefreshData(preserveScroll: true);
                    }
                    else
                    {
                        Game1.playSound("cancel");
                        log.Log("[AbilityMenu] No unspent points available.", LogLevel.Trace);
                    }
                    return;
                }
            }

            if (expandedRowKey != null)
            {
                expandedRowKey = null;
                Game1.playSound("smallSelect");
                return;
            }

            base.receiveLeftClick(x, y, playSound);
        }




        public override void receiveScrollWheelAction(int direction)
        {
            var rows = BuildRowsForDraw();
            int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);

            if (direction < 0 && scrollIndex < maxScroll)
            {
                scrollIndex++;
                Game1.playSound("shiny4");
            }
            else if (direction > 0 && scrollIndex > 0)
            {
                scrollIndex--;
                Game1.playSound("shiny4");
            }
        }



        private List<Row> BuildRowsForDraw()
        {
            var rows = new List<Row>(64);
            foreach (var g in groups)
            {
                rows.Add(new Row
                {
                    IsHeader = true,
                    HeaderText = g.ModName
                });

                foreach (var a in g.Abilities)
                {
                    bool atMax = uesApi?.IsAbilityAtMax(a.ModId, a.AbilityId) ?? false;
                    int into = 0, needed = 0, total = 0;

                    if (!atMax && uesApi != null)
                    {
   
                        var (xpInto, xpNeeded, _) = uesApi.GetAbilityProgress(a.ModId, a.AbilityId);
                        into = Math.Max(0, xpInto);
                        needed = Math.Max(0, xpNeeded);
                        total = into + needed;
                    }

                    rows.Add(new Row
                    {
                        IsHeader = false,
                        ModId = a.ModId,
                        AbilityId = a.AbilityId,
                        AbilityName = a.AbilityName,
                        Description = a.Description,
                        Level = a.Level,
                        TotalExp = a.TotalExp,
                        MaxLevel = a.MaxLevel,

         
                        AtMax = atMax,
                        XpInto = into,
                        XpNeeded = needed,
                        XpLevelTotal = total
                    });
                }
            }
            return rows;
        }



        
    }
}
