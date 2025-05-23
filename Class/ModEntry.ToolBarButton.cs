using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {
        private Rectangle skillButtonBounds;

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady) return;


            skillButtonBounds = new Rectangle(Game1.viewport.Width / 2 - 500, Game1.viewport.Height -100, 64, 64);

            // Draw button
            IClickableMenu.drawTextureBox(
                b: e.SpriteBatch,
                texture: Game1.menuTexture,
                sourceRect: new Rectangle(0, 256, 60, 60),
                x: skillButtonBounds.X,
                y: skillButtonBounds.Y,
                width: skillButtonBounds.Width,
                height: skillButtonBounds.Height,
                color: Color.White,
                drawShadow: false
            );


            string pointText = SaveData.UnspentSkillPoints.ToString();
            Vector2 textSize = Game1.smallFont.MeasureString(pointText);
            Vector2 textPos = new Vector2(
                skillButtonBounds.Center.X - textSize.X / 2,
                skillButtonBounds.Center.Y - textSize.Y / 2
            );
            e.SpriteBatch.DrawString(Game1.smallFont, pointText, textPos, Color.Black);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Toggle menu with configured hotkey
            if (e.Button == Config.ToggleMenuKey)
            {
                if (Game1.activeClickableMenu is SkillAllocationMenu)
                {
                    Game1.exitActiveMenu();
                    Game1.playSound("bigDeSelect");
                }
                else if (Game1.activeClickableMenu == null)
                {
                    Game1.activeClickableMenu = new SkillAllocationMenu(this);
                    Game1.playSound("bigSelect");
                }

                return;
            }

            // Open menu only on mouse click
            if (e.Button == SButton.MouseLeft &&
                skillButtonBounds.Contains((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y) &&
                Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new SkillAllocationMenu(this);
                Game1.playSound("bigSelect");
            }
        }




    }
}
