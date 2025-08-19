using Microsoft.Xna.Framework;
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


            public Rectangle? ButtonBounds;
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

            var result = new List<AbilityGroupVM>();
            var api = uesApi;
            var registered = uesApi?.ListRegisteredAbilities()
                 ?? Enumerable.Empty<(string modId, string abilityId, string displayName, string Description, int maxLevel)>();

            var grouped = new Dictionary<string, AbilityGroupVM>(StringComparer.OrdinalIgnoreCase);


            if (registered != null)
            {
                foreach (var (modId, abilityId, displayName, description, MaxLevel) in registered)
                {
                    //UniqueID as the header text
                    if (!grouped.TryGetValue(modId, out var g))
                    {
                        g = new AbilityGroupVM { ModId = modId, ModName = modId };
                        grouped[modId] = g;
                    }

                    long totalExp = GetTotalExpPersisted(modId, abilityId);
                    int level = api?.GetAbilityLevel(modId, abilityId) ?? 0;

                    g.Abilities.Add(new AbilityRowVM
                    {
                        ModId = modId,
                        ModName = g.ModName,
                        AbilityId = abilityId,
                        AbilityName = string.IsNullOrWhiteSpace(displayName) ? abilityId : displayName,
                        Description = description,
                        TotalExp = totalExp,
                        Level = level,
                        MaxLevel = MaxLevel
                    });
                }
            }
            else
            {
     
                var list = mod.SaveData.Abilities ?? new List<AbilitySaveData>();
                foreach (var a in list)
                {
                    var modId = a.ModGuid ?? "";
                    var abilityId = a.AbilityId ?? "";
                    if (string.IsNullOrWhiteSpace(modId) || string.IsNullOrWhiteSpace(abilityId)) continue;

                    if (!grouped.TryGetValue(modId, out var g))
                    {
                        g = new AbilityGroupVM { ModId = modId, ModName = modId }; 
                    }

                    int level = api?.GetAbilityLevel(modId, abilityId) ?? 0;

                    g.Abilities.Add(new AbilityRowVM
                    {
                        ModId = modId,
                        ModName = g.ModName,
                        AbilityId = abilityId,
                        AbilityName = abilityId,

                        TotalExp = Math.Max(0, a.TotalExpSpent),
                        Level = level
                    });
                }
            }


            foreach (var g in grouped.Values)
                g.Abilities.Sort((x, y) => string.Compare(x.AbilityName, y.AbilityName, StringComparison.OrdinalIgnoreCase));

            result.AddRange(grouped.Values);
            result.Sort((x, y) => string.Compare(x.ModName, y.ModName, StringComparison.OrdinalIgnoreCase));
            return result;
        }


        private long GetTotalExpPersisted(string modId, string abilityId)
        {
            var list = mod.SaveData.Abilities;
            if (list == null) return 0;
            foreach (var a in list)
            {
                if (a != null &&
                    string.Equals(a.ModGuid, modId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase))
                    return Math.Max(0, a.TotalExpSpent);
            }
            return 0;
        }


        // ----------------- DRAW -----------------

        public override void draw(SpriteBatch b)
        {
            _hoverRegions.Clear();
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);


            int titleY = yPositionOnScreen + 40 + yOffset;
            SpriteText.drawString(
                b,
                $"{mod.Helper.Translation.Get("ui.availablePoints")}: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );


            var rows = BuildRowsForDraw();
            int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);


            string? expandedTooltipToDraw = null;

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(RowHeight - 10, 48);

            for (int i = 0; i < MaxVisibleRows && i + scrollIndex < rows.Count; i++)
            {
                var row = rows[i + scrollIndex];
                int y = rowStartY + i * RowHeight;

                if (row.IsHeader)
                {
                    SpriteText.drawString(b, row.HeaderText, xPositionOnScreen + 40, y, color: Color.DarkBlue);
                }
                else
                {

                    var name = row.AbilityName ?? "";
                    var shortName = name.Length > 15 ? (name.Substring(0, 15)+ "..") : name;

                    if (shortName.Length < 15)
                    {
                        var spacesToAdd = 15 - shortName.Length;

                        for (int j = 0; j < spacesToAdd; j++)
                        {
                            shortName += " ";
                        }
                    }
                    


                    string text = "";

                    //Handles Row text
                    bool showPlus = !row.AtMax;  


                    text = row.AtMax ? $"{shortName} (Level: {row.Level})"
                    : $"{shortName} (Level: {row.Level})    {row.XpNeeded}";
  



                    var textRect = new Rectangle(xPositionOnScreen + 70, y, width - 200, RowHeight);

       
                    SpriteText.drawString(b, text, xPositionOnScreen + 70, y);
                    string key = $"{row.ModId}/{row.AbilityId}";
                    if (expandedRowKey == key)
                    {
                        expandedTooltipToDraw = BuildAbilityTooltip(row); // defer drawing
                    }


                    //Only draw bar if not at Max level.
                    if (showPlus)
                    {
                        // layout for Progress Bar
                        int barHeight = 50;
                        int desiredBarWidth = 300;
                        int maxBarWidth = 300;
                        int rightEdgeOffset = 50;
                        int gapFromButton = 8;

                        // vertical tweak (pixels). try -6, -8, etc.
                        int barOffsetY = -15;

                        // transparency knobs
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

                        // compute right-aligned bar rect
                        int barRight = plusBtn.Left - gapFromButton;
                        int leftTextX = xPositionOnScreen + 70;
                        int maxSpace = Math.Max(0, barRight - leftTextX);
                        int barWidth = Math.Min(Math.Max(desiredBarWidth, 0), Math.Min(maxBarWidth, maxSpace));
                        if (barWidth < 60) barWidth = Math.Min(60, maxSpace);

                        // center in row, then nudge by barOffsetY
                        int barY = y + (RowHeight - barHeight) / 2 + barOffsetY;

                        // keep it inside the row (optional clamp)
                        barY = Math.Max(y, Math.Min(barY, y + RowHeight - barHeight));

                        var barRect = new Rectangle(barRight - barWidth, barY, barWidth, barHeight);

                        // background (translucent so text under it still shows)
                        b.Draw(Game1.staminaRect, barRect, Color.Black * bgAlpha);

                        // fill (50% transparent)
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

                        // subtle 1px border (also translucent)
                        Color bc = Color.Black * borderAlpha;
                        b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y, barRect.Width, 1), bc);
                        b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y + barRect.Height - 1, barRect.Width, 1), bc);
                        b.Draw(Game1.staminaRect, new Rectangle(barRect.X, barRect.Y, 1, barRect.Height), bc);
                        b.Draw(Game1.staminaRect, new Rectangle(barRect.Right - 1, barRect.Y, 1, barRect.Height), bc);
                    }
                    




                    // draw [+] button if Ability is not at Max Level
                    if (showPlus)
                    {
                        Rectangle btn = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y - 6, buttonSize, buttonSize);
                        b.Draw(emojiTexture, btn, new Rectangle(108, 81, 9, 9), Color.White);
                    }


                    




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
                $"Discription: {desc}",
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

                // 1) click on row text toggles context help
                var textRect = new Rectangle(xPositionOnScreen + 70, rowY, width - 200, RowHeight);
                if (textRect.Contains(x, y))
                {
                    string key = $"{row.ModId}/{row.AbilityId}";
                    expandedRowKey = (expandedRowKey == key) ? null : key; // toggle
                    Game1.playSound("smallSelect");
                    return;
                }

                // 2) [+] button (only when not at max, preserves your behavior)
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
                        // spend & allocate
                        mod.AllocateAbilityPoints(row.ModId, row.AbilityId);

                        Game1.playSound("coin");
                        if (mod.Config.DebugMode)
                            log.Log($"[AbilityMenu] Allocated point to {row.ModId}/{row.AbilityId}. Remaining points: {mod.SaveData.UnspentSkillPoints}", LogLevel.Debug);

                        // refresh menu data so XP/level updates immediately
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
