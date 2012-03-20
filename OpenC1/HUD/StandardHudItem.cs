﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using OneAmEngine;

namespace OpenC1.HUD
{
    class StandardHudItem : BaseHUDItem
    {
        public override void Update()
        {
            
        }

        public override void Render()
        {
            Vector2 pos = ScaleVec2(0.22f, 0.01f);
            DrawShadow(new Rectangle((int)pos.X-5, (int)pos.Y-5, 155, 24));

            FontRenderer.Render(Fonts.White, "CP", pos, Color.White);
            pos.X += 25f;
            FontRenderer.Render(Fonts.White, String.Format("{0}/{1}", Race.Current.NextCheckpoint, Race.Current.ConfigFile.Checkpoints.Count), pos, Color.White);
            pos.X += 45f;
            FontRenderer.Render(Fonts.White, "LAP", pos, Color.White);
            pos.X += 35f;
            FontRenderer.Render(Fonts.White, String.Format("{0}/{1}", Race.Current.CurrentLap, Race.Current.ConfigFile.LapCount), pos, Color.White);

            pos = ScaleVec2(0.22f, 0.054f);

            DrawShadow(new Rectangle((int)pos.X - 5, (int)pos.Y - 5, 155, 24));
            FontRenderer.Render(Fonts.White, "WASTED", pos, Color.White);
            
            pos.X += 65f;
            FontRenderer.Render(Fonts.White, Race.Current.NbrDeadOpponents + "/" + Race.Current.NbrOpponents, pos, Color.White);

            pos.X += 240;
            DrawShadow(new Rectangle((int)pos.X - 5, (int)pos.Y - 5, 140, 24));
            FontRenderer.Render(Fonts.White, Race.Current.NbrDeadPeds + "/" + Race.Current.Peds.Count, pos, Color.White);
                        
            pos.X += 80;
            FontRenderer.Render(Fonts.White, "KILLS", pos, Color.White);
        }
    }
}
