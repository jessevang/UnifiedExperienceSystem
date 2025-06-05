using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {
        private Point dragOffset = Point.Zero;
        private int? tempButtonPosX = null;
        private int? tempButtonPosY = null;

        private int? OnMouseClickButtonPosX = null;
        private int? OnMouseClickButtonPosY = null;
        private int? OnReleaseClickButtonPosX = null;
        private int? OnReleaseClickButtonPosY = null;

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

        private Rectangle GetButtonBoundsForUI(bool forClick = false)
        {
            int baseButtonWidth = 64;
            int baseButtonHeight = 64;
            float uiScale = Game1.options.uiScale;

            int buttonWidth = (int)(baseButtonWidth * uiScale);
            int buttonHeight = (int)(baseButtonHeight * uiScale);

            int logicalX = (forClick ? Config.ButtonPosX : tempButtonPosX ?? Config.ButtonPosX) ?? 10;
            int logicalY = (forClick ? Config.ButtonPosY : tempButtonPosY ?? Config.ButtonPosY) ?? -10;

            int x = (int)(logicalX * uiScale);
            int y = logicalY >= 0
                ? (int)(logicalY * uiScale)
                : Game1.uiViewport.Height + (int)(logicalY * uiScale) - buttonHeight;

            return new Rectangle(x, y, buttonWidth, buttonHeight);
        }

        private void CheckButtonDragging()
        {
            if (OnMouseClickButtonPosX == null || OnMouseClickButtonPosY == null)
                return;

            int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
            int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

            tempButtonPosX = (int)((scaledX - dragOffset.X) / Game1.options.uiScale);
            tempButtonPosY = (int)((scaledY - dragOffset.Y) / Game1.options.uiScale);
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

            if (e.Button == SButton.MouseLeft)
            {
                int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
                int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

                Rectangle bounds = GetButtonBoundsForUI();
                if (bounds.Contains(scaledX, scaledY))
                {
                    OnMouseClickButtonPosX = scaledX;
                    OnMouseClickButtonPosY = scaledY;
                    dragOffset = new Point(scaledX - bounds.X, scaledY - bounds.Y);
                }
            }
        }

        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowSkillPointButton)
                return;

            if (e.Button != SButton.MouseLeft)
                return;

            int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
            int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

            OnReleaseClickButtonPosX = scaledX;
            OnReleaseClickButtonPosY = scaledY;

            bool clicked = (OnMouseClickButtonPosX.HasValue && OnMouseClickButtonPosY.HasValue &&
                            OnReleaseClickButtonPosX == OnMouseClickButtonPosX &&
                            OnReleaseClickButtonPosY == OnMouseClickButtonPosY);

            Rectangle skillButtonBounds = GetButtonBoundsForUI(forClick: true);

            if (clicked)
            {
                if (skillButtonBounds.Contains(scaledX, scaledY) && Game1.activeClickableMenu == null)
                {
                    Game1.activeClickableMenu = new SkillAllocationMenu(this);
                    Game1.playSound("bigSelect");
                }
            }
            else
            {
                if (tempButtonPosX.HasValue && tempButtonPosY.HasValue)
                {
                    Config.ButtonPosX = tempButtonPosX.Value;
                    Config.ButtonPosY = tempButtonPosY.Value;
                    Helper.WriteConfig(Config);
                }

                tempButtonPosX = null;
                tempButtonPosY = null;
            }

            OnMouseClickButtonPosX = null;
            OnMouseClickButtonPosY = null;
            OnReleaseClickButtonPosX = null;
            OnReleaseClickButtonPosY = null;
        }
    }
}
