﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenC1.Parsers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using OpenC1.HUD;
using Microsoft.Xna.Framework.Input;
using OneAmEngine;

namespace OpenC1.Screens
{
    
    class MainMenuScreen : BaseMenuScreen
    {

        public MainMenuScreen(BaseMenuScreen parent)
            : base(parent)
        {

            if (!SoundCache.IsInitialized)
            {
                Engine.Audio.SetDefaultVolume(-500);
                SoundCache.Initialize();

                GameVars.Palette = new PaletteFile(GameVars.BasePath + "reg\\palettes\\drrender.pal");
            }
            

            _inAnimation = new AnimationPlayer(LoadAnimation("MAI2COME.fli"), 1);
            _inAnimation.Play(false);
            ScreenEffects.Instance.FadeSpeed = 300;
            ScreenEffects.Instance.UnFadeScreen();

            _outAnimation = new AnimationPlayer(LoadAnimation("MAI2AWAY.fli"));

            _options.Add(
                new TextureMenuOption(BaseHUDItem.ScaleRect(0.181f, 0.256f, 0.68f, 0.045f),
                    LoadAnimation("MAI2N1GL.fli")[0])
            );

            _options.Add(
                new TextureMenuOption(BaseHUDItem.ScaleRect(0.180f, 0.711f, 0.715f, 0.045f),
                    LoadAnimation("MAI2QTGL.fli")[0])
            );
        }

        public override void OnOutAnimationFinished()
        {
            if (_selectedOption == 0)
                Engine.Screen = new SelectSkillScreen(this);
            else if (_selectedOption == 1)
                Engine.Game.Exit();
        }
    }
}
