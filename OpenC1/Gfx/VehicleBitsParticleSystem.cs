﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using OneAmEngine;
using Microsoft.Xna.Framework.Graphics;

namespace OpenC1.Gfx
{
    class VehicleBitsParticleSystem : ParticleSystem
    {
        CMaterial _material;

        public VehicleBitsParticleSystem(CMaterial material)
        {
            _material = material;
            InitializeSystem();
        }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.Texture = Engine.ContentManager.Load<Texture2D>("Content/blank-particle");

            settings.MaxParticles = 100;

            Color color;
            if (_material == null)
                color = Color.Black;
            else
                color = GameVars.Palette.GetRGBColorForPixel(_material.SimpMatPixelIndex);

            settings.MinColor = color;
            settings.MaxColor = color;

            settings.Duration = TimeSpan.FromSeconds(1.5f);

            settings.MinHorizontalVelocity = 2.2f;
            settings.MaxHorizontalVelocity = 3f;

            settings.MinVerticalVelocity = 1.9f;
            settings.MaxVerticalVelocity = 3f;
            settings.DurationRandomness = 0.2f;
            settings.EmitterVelocitySensitivity = 0f;

            settings.Gravity = new Vector3(0, -6f, 0);

            settings.EndVelocity = 1f;

            settings.MinStartSize = 0.2f;
            settings.MaxStartSize = 0.6f;

            settings.MinEndSize = 0.3f;
            settings.MaxEndSize = 0.6f;

            settings.MinRotateSpeed = 6f;
            settings.MaxRotateSpeed = 8f;
        }
    }
}
