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
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            // === Mini icon box on the left ===
            const int MiniW = 48;
            const int MiniH = 48;
            const int MiniGap = -20;
            const int MiniYOffset = 85;

            int miniX = xPositionOnScreen - (MiniW + MiniGap);
            int miniY = yPositionOnScreen + MiniYOffset;

            abilityIconBounds = new Rectangle(miniX, miniY, MiniW, MiniH);

            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                miniX, miniY, MiniW, MiniH, Color.White, 1f, drawShadow: false
            );

            Rectangle miniSrc = new Rectangle(0, 410, 16, 16);
            int pad = 8;
            float miniScale = Math.Min((MiniW - pad * 2f) / miniSrc.Width, (MiniH - pad * 2f) / miniSrc.Height);
            int miniIconW = (int)Math.Round(miniSrc.Width * miniScale);
            int miniIconH = (int)Math.Round(miniSrc.Height * miniScale);
            int miniIconX = miniX + (MiniW - miniIconW) / 2;
            int miniIconY = miniY + (MiniH - miniIconH) / 2;

            b.Draw(skillIconTexture, new Rectangle(miniIconX, miniIconY, miniIconW, miniIconH), miniSrc, Color.White);

            // Title
            int titleY = yPositionOnScreen + 40 + yOffset;
            SpriteText.drawString(
                b,
                $"{mod.Helper.Translation.Get("config.toggleMenuHotkeysSkill.name")} - {mod.Helper.Translation.Get("ui.availablePoints")}: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );

            // Paging
            var visibleSkills = skillList;
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(rowHeight - 10, 64);

            // layout constants per row
            const int ContentPadLeft = 40;
            const int IconTextGap = 10;
            const int RowVPad = 4;

            // prefetch vanilla cap XP for configured MaxSkillLevel
            int cfgMax = Math.Clamp(mod.Config.MaxSkillLevel, 10, 100);
            int vanillaCapXp = mod.UESgetBaseExperienceForLevel(cfgMax); // requires UESgetBaseExperienceForLevel to be public

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                int overallIndex = i + scrollIndex;
                var skill = visibleSkills[overallIndex];

                // Levels: vanilla uses raw (unmodified) + buffed in parens, SpaceCore via API as you already do
                int level;
                int buffedLevel = -1;
                if (skill.IsVanilla && int.TryParse(skill.Id, out int vanillaIdx))
                {
                    level = Game1.player.GetUnmodifiedSkillLevel(vanillaIdx); // raw (can be >10 now)
                    buffedLevel = Game1.player.GetSkillLevel(vanillaIdx);     // buffed (for parentheses)
                }
                else
                {
                    level = mod.spaceCoreApi?.GetLevelForCustomSkill(Game1.player, skill.Id) ?? 0;
                    buffedLevel = mod.spaceCoreApi?.GetBuffLevelForCustomSkill(Game1.player, skill.Id) ?? -1;
                }

                string strbuffLevel = (buffedLevel > level) ? $"({buffedLevel})" : "";
                int xp = mod.GetExperience(Game1.player, skill);

                // Per-row can-allocate check (vanilla respects configured cap, SC treated as uncapped here)
                bool canGainMoreXp =
                    skill.IsVanilla
                        ? (level < cfgMax && xp < vanillaCapXp)
                        : true; // keep SC behavior as uncapped (your backend clamps)

                int y = rowStartY + i * rowHeight;

                // Icon
                Texture2D iconTex;
                Rectangle iconSrc;
                if (skill.IsVanilla && overallIndex <= 4)
                {
                    iconTex = skillIconTexture;
                    iconSrc = VanillaSkillIcons[overallIndex];
                }
                else
                {
                    Texture2D? scTex = mod.spaceCoreApi?.GetSkillPageIconForCustomSkill(skill.Id);
                    if (scTex != null)
                    {
                        iconTex = scTex;
                        iconSrc = new Rectangle(0, 0, scTex.Width, scTex.Height);
                    }
                    else
                    {
                        iconTex = skillIconTexture;
                        iconSrc = new Rectangle(80, 0, 16, 16);
                    }
                }

                int iconMaxH = Math.Max(12, rowHeight - RowVPad * 2);
                float iconScale = iconMaxH / (float)iconSrc.Height;
                int iconW = (int)Math.Round(iconSrc.Width * iconScale);
                int iconH = (int)Math.Round(iconSrc.Height * iconScale);

                int iconX = xPositionOnScreen + ContentPadLeft;
                int baselineOffset = -16;
                int iconY = y + (rowHeight - iconH) / 2 + baselineOffset;

                b.Draw(iconTex, new Rectangle(iconX, iconY, iconW, iconH), iconSrc, Color.White);

                // Text
                int textX = iconX + iconW + IconTextGap;
                string strSkillName = (skill.DisplayName.Length > 12 ? skill.DisplayName.Substring(0, 12) : skill.DisplayName).PadRight(12);
                string strLevelRaw = $"Lv:{level}{strbuffLevel}";
                string strLevelTxt = (strLevelRaw.Length > 10 ? strLevelRaw.Substring(0, 10) : strLevelRaw).PadRight(10);
                string strXP = $"XP: {xp}";
                string finalString = $"{strSkillName}{strLevelTxt}{strXP}";
                SpriteText.drawString(b, finalString, textX, y);

                // [+] button
                if (canGainMoreXp)
                {
                    Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y, buttonSize, buttonSize);
                    b.Draw(emojiTexture, new Rectangle(buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height), new Rectangle(108, 81, 9, 9), Color.White);
                }
                // else hide when capped
            }

            if (highlightButton)
                b.Draw(Game1.staminaRect, closeButton.bounds, Color.Green * 0.3f);

            upArrow.draw(b);
            downArrow.draw(b);
            closeButton.draw(b);
            drawMouse(b);
        }







        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // ability icon -> switch menus
            if (abilityIconBounds.Contains(x, y))
            {
                if (playSound) Game1.playSound("smallSelect");
                Game1.activeClickableMenu = new AbilityAllocationMenu(mod);
                Game1.playSound("bigSelect");
                return;
            }

            var visibleSkills = skillList;
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
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

            // prefetch vanilla cap XP for configured MaxSkillLevel
            int cfgMax = Math.Clamp(mod.Config.MaxSkillLevel, 10, 100);
            int vanillaCapXp = mod.UESgetBaseExperienceForLevel(cfgMax); // requires public accessor

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int yOffsetPos = rowStartY + i * rowHeight;

                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, yOffsetPos, buttonSize, buttonSize);

                if (buttonBounds.Contains(x, y) && !skill.Id.StartsWith("Test"))
                {
                    bool canGainMoreXp;
                    if (skill.IsVanilla && int.TryParse(skill.Id, out int vanillaIdx))
                    {
                        int level = Game1.player.GetUnmodifiedSkillLevel(vanillaIdx);
                        int xp = mod.GetExperience(Game1.player, skill);
                        canGainMoreXp = (level < cfgMax && xp < vanillaCapXp);
                    }
                    else
                    {
                        // SpaceCore/custom: treat as uncapped; your backend (AddExperience) handles it
                        canGainMoreXp = true;
                    }

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


        private static void SetVanillaLevelByIndex(int which, int level)
        {
            switch (which)
            {
                case 0: Game1.player.farmingLevel.Set(level); break;
                case 1: Game1.player.fishingLevel.Set(level); break;
                case 2: Game1.player.foragingLevel.Set(level); break;
                case 3: Game1.player.miningLevel.Set(level); break;
                case 4: Game1.player.combatLevel.Set(level); break;
            }
        }



    }
}
