using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
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

            closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width / 2 - 32, yPositionOnScreen + height - 80, 64, 64),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            int arrowSize = 64; // doubled size
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

            int titleY = yPositionOnScreen + 40 + yOffset;
            SpriteText.drawString(
                b,
                $"Available Points: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                titleY
            );

            var visibleSkills = skillList.FindAll(s => !s.IsVanilla || s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            int rowStartY = titleY + 100;
            int buttonSize = Math.Min(rowHeight - 10, 64); 

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int level = mod.GetSkillLevel(Game1.player, skill);
                int xp = mod.GetExperience(Game1.player, skill);

                int y = rowStartY + i * rowHeight;
                SpriteText.drawString(b, $"{skill.DisplayName} (Lv {level}) — XP: {xp}", xPositionOnScreen + 50, y);

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
            var visibleSkills = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
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
                    mod.AllocateSkillPoint(skill.Id);
                    Game1.playSound("coin");
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
            int maxScroll = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled).Count - maxVisibleRows;
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
