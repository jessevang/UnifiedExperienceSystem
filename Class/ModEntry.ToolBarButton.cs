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
        private Rectangle currentButtonBounds;


        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ShowSkillPointButton) return;

            Rectangle finalButtonBounds = GetButtonBounds();

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



        private Rectangle GetButtonBounds()
        {

            int baseButtonWidth = 64;
            int baseButtonHeight = 64;
            int baseMargin = 10;

            int screenWidth = Game1.graphics.GraphicsDevice.Viewport.Width;
            int screenHeight = Game1.graphics.GraphicsDevice.Viewport.Height;



            float currentUiScale = Game1.options.uiScale;
            float currentZoom = Game1.options.zoomLevel;

            int buttonWidth = (int)(baseButtonWidth * currentUiScale * currentZoom);
            int buttonHeight = (int)(baseButtonHeight * currentUiScale * currentZoom);
            int margin = (int)(baseMargin * currentUiScale * currentZoom);

            int x = margin;
            int y = Game1.viewport.Height - buttonHeight - margin;

            return new Rectangle(x, y, buttonWidth, buttonHeight);
        }


        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;



            //Used for testing for adding exp to see if skill delevel  adds XXXXX exp to all skill
           /*
            if (e.Button == SButton.F10) 
            {
                if (!Context.IsWorldReady || skillList == null || skillList.Count == 0)
                    return;

                foreach (var skill in skillList)
                {
                    if (skill.IsVanilla)
                    {
                        int skillIndex = int.Parse(skill.Id);
                        Game1.player.gainExperience(skillIndex, 1000);
                        Monitor.Log($"[DEBUG] Gave 1000 XP to vanilla skill '{skill.DisplayName}'", LogLevel.Debug);
                    }
                    else if (spaceCoreApi != null)
                    {
                        spaceCoreApi.AddExperienceForCustomSkill(Game1.player, skill.Id, 1000);
                        Monitor.Log($"[DEBUG] Gave 1000 XP to custom skill '{skill.DisplayName}' (ID: {skill.Id})", LogLevel.Debug);
                    }
                }
            }

            */


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

            if (!Config.ShowSkillPointButton)
                return;

            Rectangle skillButtonBounds = GetButtonBounds();
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            //Monitor.Log($"Mouse (Click Check): {mouseX}, {mouseY} | Button Bounds (Click Check): {skillButtonBounds}", LogLevel.Debug);

     

            if (e.Button == SButton.MouseLeft &&
                skillButtonBounds.Contains(mouseX, mouseY) &&
                Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new SkillAllocationMenu(this);
                Game1.playSound("bigSelect");
            }

        }

        
        


    }
}
