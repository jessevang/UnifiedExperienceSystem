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

        private Rectangle EnergyRect
        {
            get
            {
                int w = Config.EnergyBarWidth;
                int h = Config.EnergyBarHeight;
                int vw = Game1.uiViewport.Width;
                int vh = Game1.uiViewport.Height;

                if (Config.EnergyBarUseRelativePos)
                {
                    // available travel area so the bar stays fully on screen
                    int maxX = Math.Max(0, vw - w);
                    int maxY = Math.Max(0, vh - h);

                    int x = (int)Math.Round(Config.EnergyBarRelX * maxX);
                    int y = (int)Math.Round(Config.EnergyBarRelY * maxY);
                    return new Rectangle(x, y, w, h);
                }
                else
                {
                    return new Rectangle(Config.EnergyBarX, Config.EnergyBarY, w, h);
                }
            }
        }



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

            // Always hide during events/cutscenes/festivals
            if (Game1.eventUp || Game1.CurrentEvent != null || Game1.currentLocation?.currentEvent != null)
                return;

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

            // 2) inner area (below bevels), tuned for vertical bars
            // Use the thickness (width) so border scales sensibly; clamp to keep inner positive.
            int thickness = rect.Width;
            int border = (int)Math.Round(thickness * 0.22f);
            border = Math.Clamp(border, 6, Math.Max(0, (thickness - 2) / 2));

            // 1px extra inset on all sides so the fill never touches the inner bevel
            const int safeInset = 1;
            int x = rect.X + border + safeInset;
            int y = rect.Y + border + safeInset;
            int w = Math.Max(0, rect.Width - (border * 2) - (safeInset * 2));
            int h = Math.Max(0, rect.Height - (border * 2) - (safeInset * 2));
            if (w <= 0 || h <= 0) return;

            var inner = new Rectangle(x, y, w, h);

            // (optional) recessed background to make the fill look nested
            b.Draw(Game1.staminaRect, inner, Color.Black * 0.25f);

            // 3) fill (bottom -> top)
            float pct = MathHelper.Clamp(_energy.Current / EnergyData.Max, 0f, 1f);
            int fillH = (int)Math.Round(inner.Height * pct);
            if (fillH > 0)
            {
                var fillRect = new Rectangle(inner.X, inner.Bottom - fillH, inner.Width, fillH);
                b.Draw(Game1.staminaRect, fillRect, Color.Gold);
            }

            // 4) numeric text (optional, rotated -90°, centered in inner)
            if (Config.EnergyBarShowNumeric)
            {
                string text = ((int)_energy.Current).ToString();
                Vector2 size = Game1.dialogueFont.MeasureString(text);

                // Fit rotated text within bar width
                float maxRotatedHeight = inner.Width - 6;
                float scale = MathF.Min(1f, maxRotatedHeight / Math.Max(1f, size.Y));

                Vector2 center = new(inner.X + inner.Width / 2f, inner.Y + inner.Height / 2f);
                Vector2 origin = size / 2f;

                b.DrawString(Game1.dialogueFont, text, center + new Vector2(2, 2), Color.Black * 0.4f,
                    -MathF.PI / 2f, origin, scale, SpriteEffects.None, 1f);
                b.DrawString(Game1.dialogueFont, text, center, Color.White,
                    -MathF.PI / 2f, origin, scale, SpriteEffects.None, 1f);
            }

            // 5) drag hint
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

            int vw = Game1.uiViewport.Width;
            int vh = Game1.uiViewport.Height;
            int w = Config.EnergyBarWidth;
            int h = Config.EnergyBarHeight;

            int maxX = Math.Max(0, vw - w);
            int maxY = Math.Max(0, vh - h);

            int newX = (int)e.NewPosition.ScreenPixels.X - _dragOffset.X;
            int newY = (int)e.NewPosition.ScreenPixels.Y - _dragOffset.Y;

            newX = Math.Clamp(newX, 0, maxX);
            newY = Math.Clamp(newY, 0, maxY);

            if (Config.EnergyBarUseRelativePos)
            {
                // store as 0..1 ratios
                Config.EnergyBarRelX = maxX == 0 ? 0f : (float)newX / maxX;
                Config.EnergyBarRelY = maxY == 0 ? 0f : (float)newY / maxY;
            }
            else
            {
                // legacy absolute mode
                Config.EnergyBarX = newX;
                Config.EnergyBarY = newY;
            }
        }


        private void OnButtonReleased_EnergyBar(object? sender, ButtonReleasedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft && _isDraggingEnergy)
            {
                _isDraggingEnergy = false;
                Helper.WriteConfig(Config); 
            }
        }

    }
}
