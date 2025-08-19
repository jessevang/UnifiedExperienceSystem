using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry : Mod
    {
        private EnergyData _energy = new();
        private bool _isDraggingEnergy;
        private Point _dragOffset; // cursor - barTopLeft during drag

        private Rectangle EnergyRect =>
            new Rectangle(Config.EnergyBarX, Config.EnergyBarY, Config.EnergyBarWidth, Config.EnergyBarHeight);

        private void InitEnergyMinimal()
        {
            // start full for now (no save-per-farmer yet)
            _energy.ResetFull();

            Helper.Events.Display.RenderedHud += OnRenderedHud_EnergyBar;
            Helper.Events.Input.ButtonPressed += OnButtonPressed_EnergyBar;
            Helper.Events.Input.ButtonReleased += OnButtonReleased_EnergyBar;
            Helper.Events.Input.CursorMoved += OnCursorMoved_EnergyBar;
        }

        private void OnRenderedHud_EnergyBar(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            var b = e.SpriteBatch;
            var rect = EnergyRect;

            // 1) frame
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                rect.X, rect.Y, rect.Width, rect.Height,
                Color.White,
                1f,
                drawShadow: false
            );

            // 2) inner fill area of energy bar
            int border = Math.Max(8, (int)Math.Round(rect.Height * 0.22f)); ;
            var inner = new Rectangle(
                 rect.X + border,
                 rect.Y + border+1,
                 rect.Width - border * 2,
                 (rect.Height - border * 2)-1
             );

            // 3) fill (gold/yellow)
            float pct = _energy.Current / EnergyData.Max;
            int fillW = (int)(inner.Width * pct);
            if (fillW > 0)
                b.Draw(Game1.staminaRect, new Rectangle(inner.X, inner.Y, fillW, inner.Height), Color.Gold);

            // 4) numeric text (optional)
            if (Config.EnergyBarShowNumeric)
            {
                string text = ((int)_energy.Current).ToString();
                Vector2 size = Game1.dialogueFont.MeasureString(text);
                // scale so it fits comfortably
                float scale = MathF.Min(1f, inner.Height / (size.Y + 8f));

                float tx = rect.X + rect.Width / 2f - (size.X * scale) / 2f;
                float ty = rect.Y + rect.Height / 2f - (size.Y * scale) / 2f;

                b.DrawString(Game1.dialogueFont, text, new Vector2(tx + 2, ty + 2), Color.Black * 0.4f, 0, Vector2.Zero, scale, SpriteEffects.None, 1f);
                b.DrawString(Game1.dialogueFont, text, new Vector2(tx, ty), Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 1f);
            }

            // 5) tiny hint when dragging
            if (_isDraggingEnergy)
                SpriteText.drawString(b, "Dragging (Shift+LMB)", rect.X, rect.Y - 36);
        }

        private void OnButtonPressed_EnergyBar(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Only start drag with Shift + Left Mouse
            if (e.Button != SButton.MouseLeft) return;
            if (!Helper.Input.IsDown(SButton.LeftShift) && !Helper.Input.IsDown(SButton.RightShift)) return;

            var mouse = Game1.getMousePosition();
            if (EnergyRect.Contains(mouse))
            {
                _isDraggingEnergy = true;
                _dragOffset = new Point(mouse.X - EnergyRect.X, mouse.Y - EnergyRect.Y);
            }
        }

        private void OnCursorMoved_EnergyBar(object? sender, CursorMovedEventArgs e)
        {
            if (!_isDraggingEnergy) return;

            // clamp to screen
            var screenW = Game1.uiViewport.Width;
            var screenH = Game1.uiViewport.Height;

            int newX = (int)e.NewPosition.ScreenPixels.X - _dragOffset.X;
            int newY = (int)e.NewPosition.ScreenPixels.Y - _dragOffset.Y;

            newX = Math.Clamp(newX, 0, screenW - Config.EnergyBarWidth);
            newY = Math.Clamp(newY, 0, screenH - Config.EnergyBarHeight);

            Config.EnergyBarX = newX;
            Config.EnergyBarY = newY;
        }

        private void OnButtonReleased_EnergyBar(object? sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft && _isDraggingEnergy)
            {
                _isDraggingEnergy = false;
                // persist new position immediately
                Helper.WriteConfig(Config);
            }
        }
    }
}
