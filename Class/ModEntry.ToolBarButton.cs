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
        private bool isDraggingButton = false;
        private Point dragOffset;

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowSkillPointButton)
                return;

            Rectangle skillButtonBounds = GetButtonBoundsForUI();

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

        private Rectangle GetButtonBoundsForUI()
        {
            int baseButtonWidth = 64;
            int baseButtonHeight = 64;
            int baseMargin = 10;

            float uiScale = Game1.options.uiScale;

            int buttonWidth = (int)(baseButtonWidth * uiScale);
            int buttonHeight = (int)(baseButtonHeight * uiScale);
            int margin = (int)(baseMargin * uiScale);

            int x = margin;
            int y = Game1.uiViewport.Height - buttonHeight - margin;

            return new Rectangle(x, y, buttonWidth, buttonHeight);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowSkillPointButton)
                return;

            if (Config.ToggleMenuKeys.JustPressed())
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

            if (e.Button != SButton.MouseLeft || Game1.activeClickableMenu != null)
                return;


            int scaledMouseX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
            int scaledMouseY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

            Rectangle skillButtonBounds = GetButtonBoundsForUI();

            if (skillButtonBounds.Contains(scaledMouseX, scaledMouseY))
            {
                Game1.activeClickableMenu = new SkillAllocationMenu(this);
                Game1.playSound("bigSelect");
            }
        }
    }
}
