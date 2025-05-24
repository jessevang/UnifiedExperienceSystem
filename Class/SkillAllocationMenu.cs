using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;


namespace UnifiedExperienceSystem
{
    public class SkillAllocationMenu : IClickableMenu
    {
        const int yOffset = 60;
        private readonly ModEntry mod;
        private readonly List<ClickableComponent> skillRows = new();
        private readonly List<ClickableTextureComponent> allocateButtons = new();
        private ClickableTextureComponent closeButton;



        public SkillAllocationMenu(ModEntry mod)
            : base(Game1.viewport.Width / 2 - 400, Game1.viewport.Height / 2 - 300, 800, 600, true)

        {
            this.mod = mod;

            int startY = yPositionOnScreen + 100 + yOffset;
            for (int i = 0; i < 6; i++)
            {
                string name = mod.GetSkillName(i);
                skillRows.Add(new ClickableComponent(new Rectangle(xPositionOnScreen + 50, startY + i * 60, 600, 50), name));
                allocateButtons.Add(new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + 700, startY + i * 60, 48, 48),
                    Game1.mouseCursors,
                    new Rectangle(128, 256, 64, 64),
                    0.75f
                ));
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
                if (row.name == "Unknown")
                    continue;

                if (row.name == "Luck" && !mod.Config.LuckSkillIsEnabled)
                    continue;


                int skillIndex = i;
                int level = Game1.player.GetSkillLevel(skillIndex);
                int xp = Game1.player.experiencePoints[skillIndex];

                Color textColor = Color.Brown;

                SpriteText.drawString(
                    b,
                    $"{row.name} (Lv {level}) — XP: {xp}",
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
                    mod.AllocateSkillPoint(i);
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
