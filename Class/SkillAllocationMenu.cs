using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Characters;
using StardewValley.Menus;
using System;


namespace UnifiedExperienceSystem
{
    public class SkillAllocationMenu : IClickableMenu
    {
        const int yOffset = 60;
        private int rowHeight => mod.Config.SkillMenuRowSpacing;
        private int maxVisibleRows => mod.Config.SkillMenuVisibleRows;

        private readonly ModEntry mod;
        private readonly List<SkillEntry> skillList;
        private ClickableTextureComponent closeButton;
        private ClickableTextureComponent upArrow;
        private ClickableTextureComponent downArrow;

        private int scrollIndex = 0;
        private Texture2D emojiTexture = Game1.content.Load<Texture2D>("LooseSprites/Emojis");

        private bool isHoldingButton = false;
        private float holdStartTime = 0f;
        private bool highlightButton = false;


        private Rectangle abilityIconBounds;


        //LooseSprites/cursors     used for Icon for Vanilla Skills
        private static readonly Rectangle[] VanillaSkillIcons =
{
            new Rectangle(11, 428, 8, 10),   // Farming
            new Rectangle(20, 427, 10, 11),  // Fishing
            new Rectangle(60, 427, 10, 11),  // Foraging
            new Rectangle(30, 427, 10, 11),  // Mining
            new Rectangle(120, 427, 10, 11), // Combat
        };

        private Texture2D skillIconTexture;

        public SkillAllocationMenu(ModEntry mod)
            : base(
                Game1.uiViewport.Width / 2 - mod.Config.MenuWidth / 2,
                Game1.uiViewport.Height / 2 - mod.Config.MenuHeight / 2,
                mod.Config.MenuWidth,
                mod.Config.MenuHeight,
                true)
        {
            this.mod = mod;
            this.skillList = mod.LoadAllSkills();
            skillIconTexture = Game1.content.Load<Texture2D>("LooseSprites/Cursors");

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




        public override void draw(SpriteBatch b)
        {
            // local helper: find current XP cap (respects patched curves); int.MaxValue => uncapped
            static int GetDynamicCapXp()
            {
                const int ProbeCeiling = 200; // safety limit for "infinite" curves
                int last = -1;
                for (int L = 1; L <= ProbeCeiling; L++)
                {
                    int thr = Farmer.getBaseExperienceForLevel(L);
                    if (thr < 0)
                        return (last < 0) ? 0 : last; // if somehow no levels defined, treat cap as 0
                    last = thr;
                }
                return int.MaxValue; // uncapped
            }

            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            // === Mini "icon menu" to the LEFT, aligned to main menu top ===
            const int MiniW = 48;
            const int MiniH = 48;
            const int MiniGap = -20; // horizontal: 0 = touching; negative overlaps a bit
            const int MiniYOffset = 85; // vertical offset from the menu's top

            int miniX = xPositionOnScreen - (MiniW + MiniGap);
            int miniY = yPositionOnScreen + MiniYOffset;

            abilityIconBounds = new Rectangle(miniX, miniY, MiniW, MiniH);

            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                miniX, miniY, MiniW, MiniH, Color.White, 1f, drawShadow: false
            );

            // Icon centered inside
            Rectangle miniSrc = new Rectangle(0, 410, 16, 16);
            int pad = 8;
            float miniScale = Math.Min((MiniW - pad * 2f) / miniSrc.Width, (MiniH - pad * 2f) / miniSrc.Height);
            int miniIconW = (int)Math.Round(miniSrc.Width * miniScale);
            int miniIconH = (int)Math.Round(miniSrc.Height * miniScale);
            int miniIconX = miniX + (MiniW - miniIconW) / 2;
            int miniIconY = miniY + (MiniH - miniIconH) / 2;

            b.Draw(skillIconTexture, new Rectangle(miniIconX, miniIconY, miniIconW, miniIconH), miniSrc, Color.White);
            // === End mini icon menu ===

            int titleY = yPositionOnScreen + 40 + yOffset;
            SpriteText.drawString(
                b,
                $"{mod.Helper.Translation.Get("config.toggleMenuHotkeysSkill.name")} - {mod.Helper.Translation.Get("ui.availablePoints")}: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );

            var visibleSkills = skillList;
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(rowHeight - 10, 64);

            // compute cap once per draw (global curve); works for vanilla + most mods
            int capXp = GetDynamicCapXp();
            bool isUncapped = capXp == int.MaxValue;

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                int overallIndex = i + scrollIndex;
                var skill = visibleSkills[overallIndex];

                int level;
                int buffedLevel = -1;
                if (overallIndex <= 4)
                {
                    if (int.TryParse(skill.Id, out int vanillaIdx))
                    {
                        level = Game1.player.GetUnmodifiedSkillLevel(vanillaIdx);
                        buffedLevel = Game1.player.GetSkillLevel(vanillaIdx);
                    }
                    else
                        level = 0;
                }
                else
                {
                    level = mod.spaceCoreApi?.GetLevelForCustomSkill(Game1.player, skill.Id) ?? 0;
                    buffedLevel = mod.spaceCoreApi?.GetBuffLevelForCustomSkill(Game1.player, skill.Id) ?? -1;
                }

                string strbuffLevel = (buffedLevel >= level && buffedLevel != level) ? $"({buffedLevel})" : "";
                int xp = mod.GetExperience(Game1.player, skill);

                // Should the [+] button be visible for this row?
                bool canGainMoreXp = isUncapped || xp < capXp;

                // --- layout constants ---
                const int ContentPadLeft = 40;   // inner padding from the dialog box edge
                const int IconTextGap = 10;      // space between icon and text
                const int RowVPad = 4;           // top/bottom padding inside each row

                int y = rowStartY + i * rowHeight;

                // choose icon texture + source rect
                Texture2D iconTex;
                Rectangle iconSrc;

                if (overallIndex <= 4) // VANILLA
                {
                    iconTex = skillIconTexture;
                    iconSrc = VanillaSkillIcons[overallIndex];
                }
                else // SPACECORE
                {
                    Texture2D? scTex = mod.spaceCoreApi?.GetSkillPageIconForCustomSkill(skill.Id);
                    if (scTex != null)
                    {
                        iconTex = scTex;
                        iconSrc = new Rectangle(0, 0, scTex.Width, scTex.Height);
                    }
                    else
                    {
                        // fallback placeholder from vanilla sheet if SC icon missing
                        iconTex = skillIconTexture;
                        iconSrc = new Rectangle(80, 0, 16, 16);
                    }
                }

                // scale the icon to fit inside the row height 
                int iconMaxH = Math.Max(12, rowHeight - RowVPad * 2);
                float iconScale = iconMaxH / (float)iconSrc.Height;
                int iconW = (int)Math.Round(iconSrc.Width * iconScale);
                int iconH = (int)Math.Round(iconSrc.Height * iconScale);

                // position: inside the box
                int iconX = xPositionOnScreen + ContentPadLeft;
                int baselineOffset = -16;
                int iconY = y + (rowHeight - iconH) / 2 + baselineOffset;

                // draw icon
                b.Draw(
                    iconTex,
                    new Rectangle(iconX, iconY, iconW, iconH),
                    iconSrc,
                    Color.White
                );

                // text starts after icon
                int textX = iconX + iconW + IconTextGap;

                // --- build fixed-width string ---
                string strSkillName = (skill.DisplayName.Length > 12
                    ? skill.DisplayName.Substring(0, 12)
                    : skill.DisplayName).PadRight(12);

                string strLevelRaw = $"Lv:{level}{strbuffLevel}";
                string strLevelTxt = (strLevelRaw.Length > 10
                    ? strLevelRaw.Substring(0, 10)
                    : strLevelRaw).PadRight(10);

                string strXP = $"XP: {xp}";
                string finalString = $"{strSkillName}{strLevelTxt}{strXP}";

                // draw text next to icon
                SpriteText.drawString(b, finalString, textX, y);

                // allocate button: only draw if the skill can gain more XP
                if (canGainMoreXp)
                {
                    Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y, buttonSize, buttonSize);
                    b.Draw(
                        emojiTexture,
                        new Rectangle(buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height),
                        new Rectangle(108, 81, 9, 9),
                        Color.White
                    );
                }
                // else: hide the [+] when capped
            }

            if (highlightButton)
            {
                b.Draw(Game1.staminaRect, closeButton.bounds, Color.Green * 0.3f);
            }

            upArrow.draw(b);
            downArrow.draw(b);
            closeButton.draw(b);
            drawMouse(b);
        }






        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // click on the ability icon -> switch menus
            if (abilityIconBounds.Contains(x, y))
            {
                if (playSound) Game1.playSound("smallSelect");
                Game1.activeClickableMenu = new AbilityAllocationMenu(mod);
                Game1.playSound("bigSelect");
                return;
            }

            // local helper: find current XP cap (respects patched curves); int.MaxValue => uncapped
            static int GetDynamicCapXp()
            {
                const int ProbeCeiling = 200;
                int last = -1;
                for (int L = 1; L <= ProbeCeiling; L++)
                {
                    int thr = Farmer.getBaseExperienceForLevel(L);
                    if (thr < 0)
                        return (last < 0) ? 0 : last;
                    last = thr;
                }
                return int.MaxValue; // uncapped
            }

            var visibleSkills = skillList;
            int maxScroll = visibleSkills.Count - maxVisibleRows;
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            if (upArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Max(0, scrollIndex - 1);
                Game1.playSound("shwip");
                return;
            }
            else if (downArrow.containsPoint(x, y))
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
            int buttonSize = Math.Min(rowHeight - 10, 64);

            // compute cap once per click processing
            int capXp = GetDynamicCapXp();
            bool isUncapped = capXp == int.MaxValue;

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int yOffsetPos = rowStartY + i * rowHeight;

                // the [+] button bounds for this row
                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, yOffsetPos, buttonSize, buttonSize);

                if (buttonBounds.Contains(x, y) && !skill.Id.StartsWith("Test"))
                {
                    // block allocation if at/over cap
                    int xp = mod.GetExperience(Game1.player, skill);
                    bool canGainMoreXp = isUncapped || xp < capXp;

                    if (canGainMoreXp)
                    {
                        if (mod.SaveData.UnspentSkillPoints > 0)
                        {
                            mod.AllocateSkillPoint(skill.Id);
                            Game1.playSound("coin");
                        }
                        else
                        {
                            Game1.playSound("cancel");
                        }
                    }
                    else
                    {
                        // capped: ignore click (and play cancel)
                        Game1.playSound("cancel");
                    }

                    return;
                }
            }


            base.receiveLeftClick(x, y, playSound);
        }




        public override void releaseLeftClick(int x, int y)
        {

            base.releaseLeftClick(x, y);
        }


        public override void receiveScrollWheelAction(int direction)
        {
            int maxScroll = skillList.Count;
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
    }
}
