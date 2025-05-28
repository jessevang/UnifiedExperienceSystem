using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;


namespace UnifiedExperienceSystem
{
    public class SkillAllocationMenu : IClickableMenu
    {
        const int yOffset = 60;
        const int rowHeight = 60;
        const int maxVisibleRows = 6;

        private readonly ModEntry mod;
        private readonly List<SkillEntry> skillList;
        private ClickableTextureComponent closeButton;

        private Rectangle scrollBar;
        private Rectangle scrollThumb;
        private int scrollIndex = 0;
        private bool isDragging = false;

        public SkillAllocationMenu(ModEntry mod)
            : base(Game1.viewport.Width / 2 - 400, Game1.viewport.Height / 2 - 300, 800, 600, true)
        {
            this.mod = mod;
            this.skillList = mod.LoadAllSkills();

            closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width / 2 - 32, yPositionOnScreen + height - 80, 64, 64),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
            );

            scrollBar = new Rectangle(xPositionOnScreen + width - 30, yPositionOnScreen + 100, 20, maxVisibleRows * rowHeight);
            UpdateScrollThumb();
        }

        private void UpdateScrollThumb()
        {
            int totalRows = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled).Count;
            int scrollAreaHeight = maxVisibleRows * rowHeight;
            float thumbHeightRatio = (float)maxVisibleRows / totalRows;
            int thumbHeight = (int)(scrollAreaHeight * thumbHeightRatio);
            thumbHeight = MathHelper.Clamp(thumbHeight, 20, scrollAreaHeight);

            int maxScroll = totalRows - maxVisibleRows;
            int y = scrollBar.Y + (maxScroll == 0 ? 0 : scrollIndex * (scrollAreaHeight - thumbHeight) / maxScroll);

            scrollThumb = new Rectangle(scrollBar.X, y, scrollBar.Width, thumbHeight);
        }

        public override void draw(SpriteBatch b)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            SpriteText.drawString(
                b,
                $"Available Points: {mod.SaveData.UnspentSkillPoints}",
                xPositionOnScreen + 50,
                yPositionOnScreen + 40 + yOffset
            );

            var visibleSkills = skillList.FindAll(s => s.IsVanilla == false || s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int level = mod.GetSkillLevel(Game1.player, skill);
                int xp = mod.GetExperience(Game1.player, skill);

                int y = yPositionOnScreen + 100 + yOffset + i * rowHeight;
                SpriteText.drawString(b, $"{skill.DisplayName} (Lv {level}) — XP: {xp}", xPositionOnScreen + 50, y);

                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - 80, y, 48, 48);
                b.Draw(Game1.mouseCursors, buttonBounds, new Rectangle(128, 256, 64, 64), Color.White);
            }

            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(403, 383, 6, 6), scrollBar.X, scrollBar.Y, scrollBar.Width, scrollBar.Height, Color.White);
            b.Draw(Game1.mouseCursors, scrollThumb, new Rectangle(435, 463, 6, 10), Color.White);

            closeButton.draw(b);
            drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (scrollThumb.Contains(x, y))
            {
                isDragging = true;
                Game1.playSound("shiny4");
            }

            if (closeButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }

            var visibleSkills = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int yOffsetPos = yPositionOnScreen + 100 + yOffset + i * rowHeight;
                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - 80, yOffsetPos, 48, 48);
                if (buttonBounds.Contains(x, y) && !skill.Id.StartsWith("Test"))
                {
                    mod.AllocateSkillPoint(skill.Id);
                    Game1.playSound("coin");
                }
            }

            base.receiveLeftClick(x, y, playSound);
        }

        public override void receiveScrollWheelAction(int direction)
        {
            int maxScroll = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled).Count - maxVisibleRows;
            if (direction < 0 && scrollIndex < maxScroll)
            {
                scrollIndex++;
                UpdateScrollThumb();
                Game1.playSound("shiny4");
            }
            else if (direction > 0 && scrollIndex > 0)
            {
                scrollIndex--;
                UpdateScrollThumb();
                Game1.playSound("shiny4");
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            isDragging = false;
            base.releaseLeftClick(x, y);
        }

        public override void leftClickHeld(int x, int y)
        {
            if (isDragging)
            {
                int scrollAreaHeight = maxVisibleRows * rowHeight;
                int totalRows = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled).Count;
                int maxScroll = totalRows - maxVisibleRows;
                int thumbHeight = scrollThumb.Height;
                int relativeY = MathHelper.Clamp(y - scrollBar.Y - thumbHeight / 2, 0, scrollAreaHeight - thumbHeight);

                scrollIndex = (int)((float)relativeY / (scrollAreaHeight - thumbHeight) * maxScroll);
                scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);
                UpdateScrollThumb();
            }

            base.leftClickHeld(x, y);
        }
    }
}
