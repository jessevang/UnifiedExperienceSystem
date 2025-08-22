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

        private static string TruncateSpriteText(string s, int maxWidthPx)
        {
            // trim characters until it fits (no padding, no ellipsis)
            while (s.Length > 0 && SpriteText.getWidthOfString(s) > maxWidthPx)
                s = s.Substring(0, s.Length - 1);
            return s;
        }


        public override void draw(SpriteBatch b)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);


            // === Mini "icon menu" to the LEFT, aligned to main menu top ===
            const int MiniW = 48;
            const int MiniH = 48;
            const int MiniGap = -20;//0        // horizontal: distance from main menu edge (0 = touching)
            const int MiniYOffset = 85;//0    // vertical: offset relative to main menu top (0 = same top)

            int miniX = xPositionOnScreen - (MiniW + MiniGap); // move horizontally by changing MiniGap
            int miniY = yPositionOnScreen + MiniYOffset;       // move vertically by changing MiniYOffset

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
                $"{mod.Helper.Translation.Get("ui.availablePoints")}: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );

            var visibleSkills = skillList;
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(rowHeight - 10, 64);

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

                string strbuffLevel = (buffedLevel >= 0 && buffedLevel != level) ? $"({buffedLevel})" : "";
                int xp = mod.GetExperience(Game1.player, skill);

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
                string strLevel = (strLevelRaw.Length > 10
                    ? strLevelRaw.Substring(0, 10)
                    : strLevelRaw).PadRight(10);

                string strXP = $"XP: {xp}";
                string finalString = $"{strSkillName}{strLevel}{strXP}";

                // draw text next to icon
                SpriteText.drawString(b, finalString, textX, y);

                // allocate button (unchanged)
                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, y, buttonSize, buttonSize);
                b.Draw(
                    emojiTexture,
                    new Rectangle(buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height),
                    new Rectangle(108, 81, 9, 9),
                    Color.White
                );
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
            // check if clicked on the ability icon
            if (abilityIconBounds.Contains(x, y))
            {
                if (playSound) Game1.playSound("smallSelect");
                Game1.activeClickableMenu = new AbilityAllocationMenu(mod); // <-- replace with your abilities menu class if different
                Game1.playSound("bigSelect");
                return;
            }

            var visibleSkills = skillList;
            int maxScroll = visibleSkills.Count - maxVisibleRows;
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            if (upArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Max(0, scrollIndex - 1);
                Game1.playSound("shwip");
            }
            else if (downArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Min(maxScroll, scrollIndex + 1);
                Game1.playSound("shwip");
            }

            if (closeButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }

            int titleY = yPositionOnScreen + 40 + yOffset;
            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(rowHeight - 10, 64);

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int yOffsetPos = rowStartY + i * rowHeight;
                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - buttonSize - 50, yOffsetPos, buttonSize, buttonSize);

                if (buttonBounds.Contains(x, y) && !skill.Id.StartsWith("Test"))
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
