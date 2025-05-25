using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System.Collections.Generic;

namespace UnifiedExperienceSystem
{
    public class SkillAllocationMenu : IClickableMenu
    {
        const int yOffset = 60;
        private readonly ModEntry mod;
        private readonly List<SkillEntry> skillList;
        private readonly List<ClickableComponent> skillRows = new();
        private readonly List<ClickableTextureComponent> allocateButtons = new();
        private ClickableTextureComponent closeButton;

        public SkillAllocationMenu(ModEntry mod)
            : base(Game1.viewport.Width / 2 - 400, Game1.viewport.Height / 2 - 300, 800, 600, true)
        {
            this.mod = mod;
            this.skillList = mod.LoadAllSkills(); 

            int startY = yPositionOnScreen + 100 + yOffset;
            int visibleIndex = 0;

            foreach (var skill in skillList)
            {
                if (skill.DisplayName == "Luck" && !mod.Config.LuckSkillIsEnabled)
                    continue;

                skillRows.Add(new ClickableComponent(
                    new Rectangle(xPositionOnScreen + 50, startY + visibleIndex * 60, 600, 50),
                    skill.Id // use Id to match on click
                ));

                allocateButtons.Add(new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + 700, startY + visibleIndex * 60, 48, 48),
                    Game1.mouseCursors,
                    new Rectangle(128, 256, 64, 64),
                    0.75f
                ));

                visibleIndex++;
            }

            closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width / 2 - 32, yPositionOnScreen + height - 80, 64, 64),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                4f
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

            for (int i = 0; i < skillRows.Count; i++)
            {
                var row = skillRows[i];
                var skill = skillList.Find(s => s.Id == row.name);
                if (skill == null)
                    continue;

                int level = mod.GetSkillLevel(Game1.player, skill);
                int xp = mod.GetExperience(Game1.player, skill);

                Color textColor = Color.Brown;

                SpriteText.drawString(
                    b,
                    $"{skill.DisplayName} (Lv {level}) — XP: {xp}",
                    row.bounds.X,
                    row.bounds.Y,
                    color: textColor
                );

                allocateButtons[i].draw(b);
            }

            closeButton.draw(b);
            drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            for (int i = 0; i < allocateButtons.Count; i++)
            {
                if (allocateButtons[i].containsPoint(x, y))
                {
                    var skillId = skillRows[i].name;
                    mod.AllocateSkillPoint(skillId);
                    Game1.playSound("coin");
                }
            }

            if (closeButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }

            base.receiveLeftClick(x, y, playSound);
        }
    }
}
