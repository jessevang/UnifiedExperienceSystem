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
        private int rowHeight => mod.Config.SkillMenuRowSpacing;
        private int maxVisibleRows => mod.Config.SkillMenuVisibleRows;

        private readonly ModEntry mod;
        private readonly List<SkillEntry> skillList;
        private ClickableTextureComponent closeButton;
        private ClickableTextureComponent upArrow;
        private ClickableTextureComponent downArrow;

        private int scrollIndex = 0;
        private Texture2D emojiTexture = Game1.content.Load<Texture2D>("LooseSprites/Emojis");

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

            int arrowSize = (int)(32 * 1.5f);
            int arrowX = xPositionOnScreen + width - arrowSize - 48;
            int arrowYOffset = 75;

            upArrow = new ClickableTextureComponent(
                new Rectangle(arrowX, yPositionOnScreen + arrowYOffset + 40, arrowSize, arrowSize),
                Game1.mouseCursors,
                new Rectangle(421, 459, 11, 12),
                3f
            );
            downArrow = new ClickableTextureComponent(
                new Rectangle(arrowX, upArrow.bounds.Bottom + 8, arrowSize, arrowSize),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                3f
            );
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

            var visibleSkills = skillList.FindAll(s => !s.IsVanilla || s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
            int maxScroll = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScroll);

            int rowStartY = downArrow.bounds.Bottom + 20;
            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int level = mod.GetSkillLevel(Game1.player, skill);
                int xp = mod.GetExperience(Game1.player, skill);

                int y = rowStartY + i * rowHeight;
                SpriteText.drawString(b, $"{skill.DisplayName} (Lv {level}) — XP: {xp}", xPositionOnScreen + 50, y);

                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - 120, y, 48, 48);
                b.Draw(
                    emojiTexture,
                    new Rectangle(buttonBounds.X, buttonBounds.Y, buttonBounds.Width, buttonBounds.Height),
                    new Rectangle(108, 81, 9, 9),
                    Color.White
                );
            }

            upArrow.draw(b);
            downArrow.draw(b);
            closeButton.draw(b);
            drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (upArrow.containsPoint(x, y))
            {
                scrollIndex = Math.Max(0, scrollIndex - 1);
                Game1.playSound("shwip");
            }
            else if (downArrow.containsPoint(x, y))
            {
                int maxScroll = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled).Count - maxVisibleRows;
                scrollIndex = Math.Min(maxScroll, scrollIndex + 1);
                Game1.playSound("shwip");
            }

            if (closeButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }

            var visibleSkills = skillList.FindAll(s => s.DisplayName != "Luck" || mod.Config.LuckSkillIsEnabled);
            int maxScrollIndex = Math.Max(0, visibleSkills.Count - maxVisibleRows);
            scrollIndex = MathHelper.Clamp(scrollIndex, 0, maxScrollIndex);

            int rowStartY = downArrow.bounds.Bottom + 20;
            for (int i = 0; i < maxVisibleRows && i + scrollIndex < visibleSkills.Count; i++)
            {
                var skill = visibleSkills[i + scrollIndex];
                int yOffsetPos = rowStartY + i * rowHeight;
                Rectangle buttonBounds = new Rectangle(xPositionOnScreen + width - 120, yOffsetPos, 48, 48);
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
