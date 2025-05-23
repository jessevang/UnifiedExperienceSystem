using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

            //  Use uiViewport so position is not affected by UI scaling
            skillButtonBounds = new Rectangle(Game1.uiViewport.Width / 2 - 500, Game1.uiViewport.Height - 100, 64, 64);

            //  Draw brown menu-style box as button
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

            //  Draw centered point number
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

            //  Toggle menu with hotkey
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

            //  Match mouse input space to skillButtonBounds space (scale-aware)
            float scale = Game1.options.uiScale;
            int scaledMouseX = (int)(e.Cursor.ScreenPixels.X / scale);
            int scaledMouseY = (int)(e.Cursor.ScreenPixels.Y / scale);

            if (e.Button == SButton.MouseLeft &&
                skillButtonBounds.Contains(scaledMouseX, scaledMouseY) &&
                Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new SkillAllocationMenu(this);
                Game1.playSound("bigSelect");
            }
        }

    }
}
