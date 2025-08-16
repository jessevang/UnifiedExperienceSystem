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
            public int maxLevel;
            public int Level;
            public long TotalExp;
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


        private void RefreshData()
        {
            groups = BuildAbilityListingForUi();
            scrollIndex = 0;
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
                    string text = $"{row.AbilityName}(Lv{row.Level}) XP: {row.TotalExp}";


                    var textRect = new Rectangle(xPositionOnScreen + 70, y, width - 200, RowHeight);
                    _hoverRegions.Add((textRect, BuildAbilityTooltip(row)));

       
                    SpriteText.drawString(b, text, xPositionOnScreen + 70, y);

                    // draw [+] button
                    Rectangle btn = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y - 6, buttonSize, buttonSize);
                    b.Draw(emojiTexture, btn, new Rectangle(108, 81, 9, 9), Color.White);
                    row.ButtonBounds = btn;


                    _hoverRegions.Add((btn, BuildPlusTooltip(row)));
                }

            }

            upArrow.draw(b);
            downArrow.draw(b);
            closeButton.draw(b);
            drawMouse(b);
            if (!string.IsNullOrEmpty(_hoverText))
                IClickableMenu.drawHoverText(b, _hoverText, Game1.smallFont);
        }

        private string BuildAbilityTooltip(Row row)
        {
            var desc = string.IsNullOrEmpty(row.Description) ? "" : $"\n{row.Description}";
            return Game1.parseText(
                $"Mod: {row.ModId}\n" +
                $"Ability Name: {row.AbilityName}\n" +
                $"Level: {row.Level}\n" +
                $"MaxLevel: {row.maxLevel}\n" +
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
            foreach (var (bounds, tip) in _hoverRegions)
            {
                if (bounds.Contains(x, y))
                {
                    _hoverText = tip;
                    break;
                }
            }
            base.performHoverAction(x, y);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            var rows = BuildRowsForDraw();
            int maxScroll = Math.Max(0, rows.Count - MaxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

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


            int titleY = yPositionOnScreen + 40 + yOffset;
            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(RowHeight - 10, 48);

            for (int i = 0; i < MaxVisibleRows && i + scrollIndex < rows.Count; i++)
            {
                var row = rows[i + scrollIndex];
                if (row.IsHeader) continue;

                Rectangle btn = new Rectangle(xPositionOnScreen + width - buttonSize - 50, rowStartY + i * RowHeight - 6, buttonSize, buttonSize);
                if (btn.Contains(x, y))
                {
                    Game1.playSound("coin");
                    log.Log($"[AbilityMenu] Clicked + on {row.ModId}/{row.AbilityId}", LogLevel.Debug);

                    return;
                }
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
                    rows.Add(new Row
                    {
                        IsHeader = false,
                        ModId = a.ModId,
                        AbilityId = a.AbilityId,
                        AbilityName = a.AbilityName,
                        Description = a.Description,
                        Level = a.Level,
                        TotalExp = a.TotalExp
                    });
                }
            }
            return rows;
        }
    }
}
