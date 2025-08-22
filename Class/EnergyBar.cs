using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System.Diagnostics.Metrics;

namespace UnifiedExperienceSystem
{
    public partial class ModEntry : Mod
    {
        private EnergyData _energy = new();
        private bool _isDraggingEnergy;
        private Point _dragOffset; 
        private int _regenCounter;
        private const string SpendSfx = "sandyStep";
        private const string EmptySfx = "cancel";
        private int CanRegenerateEnergyCounter { get; set; } = 0;
        private bool CanRegenerateEnergy = true;
        internal float GetCurrentEnergyInternal() => _energy.Current;

        internal bool TryToUseAbility(float energyCost)
        {
            
            bool Successful = _energy.TrySpend(energyCost);
            if (Successful)
            {
                this.CanRegenerateEnergy = false;
                CanRegenerateEnergyCounter = 0;
                return Successful;
            }
            return Successful;

        }
        private Rectangle EnergyRect
        {
            get
            {
                int w = Config.EnergyBarWidth;
                int h = Config.EnergyBarHeight;

                if (Config.EnergyBarFollowVanillaHud)
                {
                    var target = GetAnchorTargetRect();

                    // Position = target's top-left + configured offsets
                    int x = target.X + Config.EnergyBarAnchorOffsetX;
                    int y = target.Y + Config.EnergyBarAnchorOffsetY;

                    // Clamp to safe area so it never goes off-screen
                    Rectangle safe = Utility.getSafeArea();
                    x = Math.Clamp(x, safe.X, safe.Right - w);
                    y = Math.Clamp(y, safe.Y, safe.Bottom - h);

                    return new Rectangle(x, y, w, h);
                }

                // --- your existing modes as fallback ---
                if (Config.EnergyBarUseRelativePos)
                {
                    Rectangle safe = Utility.getSafeArea();
                    int maxX = Math.Max(0, safe.Width - w);
                    int maxY = Math.Max(0, safe.Height - h);
                    int x = safe.X + (int)Math.Round(Config.EnergyBarRelX * maxX);
                    int y = safe.Y + (int)Math.Round(Config.EnergyBarRelY * maxY);
                    return new Rectangle(x, y, w, h);
                }
                else
                {
                    return new Rectangle(Config.EnergyBarX, Config.EnergyBarY, w, h);
                }
            }
        }

        private static Rectangle GetVanillaHealthFrameRect()
        {
            // Vanilla draws the health frame near the bottom-right.
            // These constants mirror vanilla HUD placement closely enough; your offset config fine-tunes.
            Rectangle safe = Utility.getSafeArea();
            int x = safe.Right - 96;  // top-left X of health frame
            int y = safe.Bottom - 160; // top-left Y of health frame
            return new Rectangle(x, y, 60, 172); // (frame size used by vanilla texture box)
        }

        private static Rectangle GetVanillaStaminaFrameRect()
        {
            Rectangle safe = Utility.getSafeArea();
            int x = safe.Right - 200;      // top-left X of stamina frame
            int y = safe.Bottom - (96 + 64); // top-left Y of stamina frame
            return new Rectangle(x, y, 172, 60);
        }

        private Rectangle GetAnchorTargetRect()
        {
            return (Config.EnergyBarAnchorTarget?.Equals("Stamina", StringComparison.OrdinalIgnoreCase) == true)
                ? GetVanillaStaminaFrameRect()
                : GetVanillaHealthFrameRect();
        }





        private void InitEnergyMinimal()
        {
            _energy.ResetFull();


            Helper.Events.Display.RenderingHud += OnRenderingHud_EnergyBar;
            Helper.Events.Input.ButtonPressed += OnButtonPressed_EnergyBar;
            Helper.Events.Input.ButtonReleased += OnButtonReleased_EnergyBar;
            Helper.Events.Input.CursorMoved += OnCursorMoved_EnergyBar;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked_Energy;
        }

        private void OnUpdateTicked_Energy(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            /*
             if (!e.IsOneSecond)
                 return;
            */


            if (!CanRegenerateEnergy)
            {
                CanRegenerateEnergyCounter++;
            }

            if (CanRegenerateEnergy)
            {
                if (e.IsOneSecond)
                {
                    float regenAmount = 5f;

                    if (_energy.Current < EnergyData.Max && _energy.Current > (EnergyData.Max - regenAmount))
                        _energy.Set(100f);
                    if (_energy.Current <= EnergyData.Max)
                        _energy.Add(regenAmount);
                }

            }

            if (CanRegenerateEnergyCounter == 120) 
            {
                CanRegenerateEnergyCounter = 0;
                CanRegenerateEnergy = true;
            }




        }


        private void OnRenderingHud_EnergyBar(object? sender, RenderingHudEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            if (Game1.CurrentEvent != null
             || Game1.eventUp
             || Game1.dialogueUp
             || Game1.currentLocation?.currentEvent != null
             || Game1.activeClickableMenu != null
             || Game1.currentMinigame != null
             || Game1.isFestival())
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

            // 2) inner area (below bevels)
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

            Rectangle safe = Utility.getSafeArea();
            int w = Config.EnergyBarWidth;
            int h = Config.EnergyBarHeight;

            // new absolute top-left, before clamping
            int newAbsX = (int)e.NewPosition.ScreenPixels.X - _dragOffset.X;
            int newAbsY = (int)e.NewPosition.ScreenPixels.Y - _dragOffset.Y;

            // clamp inside safe area
            newAbsX = Math.Clamp(newAbsX, safe.X, safe.Right - w);
            newAbsY = Math.Clamp(newAbsY, safe.Y, safe.Bottom - h);

            if (Config.EnergyBarFollowVanillaHud)
            {
                var target = GetAnchorTargetRect();
                // store offsets from the target's top-left
                Config.EnergyBarAnchorOffsetX = newAbsX - target.X;
                Config.EnergyBarAnchorOffsetY = newAbsY - target.Y;
            }
            else if (Config.EnergyBarUseRelativePos)
            {
                int maxX = Math.Max(0, safe.Width - w);
                int maxY = Math.Max(0, safe.Height - h);
                Config.EnergyBarRelX = maxX == 0 ? 0f : (float)(newAbsX - safe.X) / maxX;
                Config.EnergyBarRelY = maxY == 0 ? 0f : (float)(newAbsY - safe.Y) / maxY;
            }
            else
            {
                Config.EnergyBarX = newAbsX;
                Config.EnergyBarY = newAbsY;
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
