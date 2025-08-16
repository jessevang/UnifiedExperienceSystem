using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry
    {
        // --- Ability toolbar button state (keep names consistent!) ---
        private Point abilityDragOffset = Point.Zero;
        private int? abilityTempButtonPosX = null;
        private int? abilityTempButtonPosY = null;

        private int? abilityOnMouseDownX = null;
        private int? abilityOnMouseDownY = null;
        private int? abilityOnMouseUpX = null;
        private int? abilityOnMouseUpY = null;

        private bool abilityIsHolding = false;
        private float abilityHoldTimer = 0f;
        private const float AbilityHoldDelaySeconds = 1.0f;

        // Call this once in Entry(...)
        private void HookAbilityToolbarEvents(IModHelper helper)
        {
            helper.Events.Display.RenderedHud += AbilityOnRenderedHud;
            helper.Events.Input.ButtonPressed += AbilityOnButtonPressed;
            helper.Events.Input.ButtonReleased += AbilityOnButtonReleased;
            helper.Events.GameLoop.UpdateTicked += AbilityOnUpdateTicked;
        }

        private void AbilityOnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowAbilityButton)
                return;

            // update hold timer while holding
            if (abilityIsHolding)
            {
                abilityHoldTimer += (float)Game1.currentGameTime.ElapsedGameTime.TotalSeconds;
                AbilityCheckButtonDragging();
            }
        }

        private void AbilityOnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowAbilityButton)
                return;

            Rectangle bounds = AbilityGetButtonBoundsForUI();

            Color buttonColor = abilityIsHolding && abilityHoldTimer >= 0.25f ? Color.Green : Color.MistyRose;

            IClickableMenu.drawTextureBox(
                b: e.SpriteBatch,
                texture: Game1.menuTexture,
                sourceRect: new Rectangle(0, 256, 60, 60),
                x: bounds.X,
                y: bounds.Y,
                width: bounds.Width,
                height: bounds.Height,
                color: buttonColor,
                drawShadow: false
            );

            string pointText = SaveData.UnspentSkillPoints.ToString();
            Vector2 textSize = Game1.smallFont.MeasureString(pointText);
            Vector2 textPos = new(
                bounds.Center.X - textSize.X / 2,
                bounds.Center.Y - textSize.Y / 2
            );
            e.SpriteBatch.DrawString(Game1.smallFont, pointText, textPos, Color.Black);
        }

        private Rectangle AbilityGetButtonBoundsForUI(bool forClick = false)
        {
            int baseButtonWidth = 64;
            int baseButtonHeight = 64;
            float uiScale = Game1.options.uiScale;

            int buttonWidth = (int)(baseButtonWidth * uiScale);
            int buttonHeight = (int)(baseButtonHeight * uiScale);

            // NOTE: reusing your existing Config.ButtonPosX/Y and temp vars.
            // If you want separate ability button position, add Config.AbilityButtonPosX/Y and swap here.
            int logicalX = (forClick ? Config.AbilityButtonPosX : abilityTempButtonPosX ?? Config.AbilityButtonPosX) ?? 10;
            int logicalY = (forClick ? Config.AbilityButtonPosY : abilityTempButtonPosY ?? Config.AbilityButtonPosY) ?? -10;

            int x = (int)(logicalX * uiScale);
            int y = logicalY >= 0
                ? (int)(logicalY * uiScale)
                : Game1.uiViewport.Height + (int)(logicalY * uiScale) - buttonHeight;

            return new Rectangle(x, y, buttonWidth, buttonHeight);
        }

        private void AbilityCheckButtonDragging()
        {
            if (abilityOnMouseDownX == null || abilityOnMouseDownY == null)
                return;

            int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
            int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

            abilityTempButtonPosX = (int)((scaledX - abilityDragOffset.X) / Game1.options.uiScale);
            abilityTempButtonPosY = (int)((scaledY - abilityDragOffset.Y) / Game1.options.uiScale);
        }

        private void AbilityOnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // OPTIONAL: toggle menu hotkey reuse (adjust to your ability UI later)
            if (Config.ToggleMenuKeys.JustPressed())
            {
                if (Game1.activeClickableMenu is AbilityAllocationMenu)
                {
                    Game1.exitActiveMenu();
                    Game1.playSound("bigDeSelect");
                }
                else if (Game1.activeClickableMenu == null)
                {
                    Game1.activeClickableMenu = new AbilityAllocationMenu(this);
                    Game1.playSound("bigSelect");
                }
                return;
            }

            if (!Config.ShowAbilityButton)
                return;

            if (e.Button == SButton.MouseLeft)
            {
                int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
                int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

                Rectangle bounds = AbilityGetButtonBoundsForUI();
                if (bounds.Contains(scaledX, scaledY))
                {
                    abilityOnMouseDownX = scaledX;
                    abilityOnMouseDownY = scaledY;
                    abilityDragOffset = new Point(scaledX - bounds.X, scaledY - bounds.Y);
                    abilityIsHolding = true;
                    abilityHoldTimer = 0f;
                }
            }
        }

        private void AbilityOnButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowAbilityButton)
                return;

            if (e.Button != SButton.MouseLeft)
                return;

            int scaledX = (int)(Game1.getMouseXRaw() / Game1.options.uiScale);
            int scaledY = (int)(Game1.getMouseYRaw() / Game1.options.uiScale);

            abilityOnMouseUpX = scaledX;
            abilityOnMouseUpY = scaledY;

            bool clicked = abilityOnMouseDownX.HasValue && abilityOnMouseDownY.HasValue &&
                           Math.Abs(abilityOnMouseUpX.Value - abilityOnMouseDownX.Value) <= 20 &&
                           Math.Abs(abilityOnMouseUpY.Value - abilityOnMouseDownY.Value) <= 20;

            Rectangle bounds = AbilityGetButtonBoundsForUI(forClick: true);

            if (clicked && bounds.Contains(scaledX, scaledY))
            {
                Helper.Input.Suppress(SButton.MouseLeft);

                if (Game1.activeClickableMenu is AbilityAllocationMenu)
                {
                    Game1.exitActiveMenu();
                    Game1.playSound("bigDeSelect");
                }
                else if (Game1.activeClickableMenu == null)
                {
                    Game1.activeClickableMenu = new AbilityAllocationMenu(this); // placeholder
                    Game1.playSound("bigSelect");
                }
            }
            else
            {
                // drop → persist new position
                if (abilityTempButtonPosX.HasValue && abilityTempButtonPosY.HasValue)
                {
                    Config.AbilityButtonPosX = abilityTempButtonPosX.Value; // swap to Config.AbilityButtonPosX if you split later
                    Config.AbilityButtonPosY = abilityTempButtonPosY.Value;
                    Helper.WriteConfig(Config);
                }

                abilityTempButtonPosX = null;
                abilityTempButtonPosY = null;
            }

            // reset state
            abilityIsHolding = false;
            abilityHoldTimer = 0f;
            abilityOnMouseDownX = null;
            abilityOnMouseDownY = null;
            abilityOnMouseUpX = null;
            abilityOnMouseUpY = null;
        }
    }
}
