using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenC1.Parsers;
using Microsoft.Xna.Framework.Input;
using OpenC1.Gfx;
using System.Diagnostics;
using OpenC1.CameraViews;
using OpenC1.Physics;
using System.IO;
using OneAmEngine;
using OpenC1.Screens;
using OpenC1.GameModes;
using Microsoft.Xna.Framework.Storage;


namespace OpenC1
{
    class PlayGameScreen : IGameScreen
    {
        public IGameScreen Parent { get; private set; }

        Race _race;
        BasicEffect2 _effect;   
        List<GameMode> _modes = new List<GameMode>();
       
        int _currentEditMode = 0;

        public PlayGameScreen(IGameScreen parent)
        {
            Parent = parent;
            GC.Collect();

            _race = new Race(GameVars.BasePath + "races\\" + GameVars.SelectedRaceInfo.RaceFilename, GameVars.SelectedCarFileName);
            
            _modes.Add(new NormalMode());
            _modes.Add(new FlyMode());
            _modes.Add(new OpponentEditMode());
            _modes.Add(new PedEditMode());
            GameMode.Current = _modes[_currentEditMode];
        }


        public void Update()
        {
            PhysX.Instance.Simulate();
            PhysX.Instance.Fetch();

            _race.Update();

            foreach (ParticleSystem system in ParticleSystem.AllParticleSystems)
                system.Update();

            if (Engine.Input.WasPressed(Keys.Escape))
            {
                Engine.Screen = new PauseMenuScreen(this);
                return;
            }

            if (Engine.Input.WasPressed(Keys.F4))
            {
                _currentEditMode = (_currentEditMode + 1) % _modes.Count;
                GameMode.Current = _modes[_currentEditMode];
            }
            if (Engine.Input.WasPressed(Keys.P))
            {
                TakeScreenshot();
                //MessageRenderer.Instance.PostMainMessage("destroy.pix", 50, 0.7f, 0.003f, 1.4f);
            }
            if (Engine.Input.WasPressed(Keys.L))
            {
                GameVars.LightingEnabled = !GameVars.LightingEnabled;
                MessageRenderer.Instance.PostHeaderMessage("Lighting: " + (GameVars.LightingEnabled ? "Enabled" : "Disabled"), 2);
                _effect = null;
            }
                        
            GameMode.Current.Update();
            _race.PlayerVehicle.Chassis.OutputDebugInfo();

            Engine.Camera.Update();
            
            GameConsole.WriteLine("FPS", Engine.Fps);   
        }

        public void Render()
        {
            Engine.Device.Clear(GameVars.FogColor);
            GameVars.NbrDrawCalls = 0;
            if (GameVars.CullingDisabled)
                Engine.Device.RenderState.CullMode = CullMode.None;
            else
                Engine.Device.RenderState.CullMode = CullMode.CullClockwiseFace;

            GameVars.CurrentEffect = SetupRenderEffect();

            GameVars.NbrSectionsChecked = GameVars.NbrSectionsRendered = 0;

            Engine.SpriteBatch.Begin();

            _race.Render();
            _modes[_currentEditMode].Render();

            foreach (ParticleSystem system in ParticleSystem.AllParticleSystems)
            {
                system.Render();
            }

            Engine.SpriteBatch.End();
            Engine.Device.RenderState.DepthBufferEnable = true;
            Engine.Device.RenderState.AlphaBlendEnable = false;
            Engine.Device.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            Engine.Device.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

            GameVars.CurrentEffect.End();

            GameConsole.WriteLine("Position", Race.Current.PlayerVehicle.GetBodyBottom() / 6);
                       

            GameConsole.WriteLine("Draw Calls", GameVars.NbrDrawCalls);

            OpenC1.Physics.PhysX.Instance.Draw();
        }



        private BasicEffect2 SetupRenderEffect()
        {
            GraphicsDevice device = Engine.Device;

            if (_effect == null)
            {
                
                _effect = new BasicEffect2();

                Engine.Device.RenderState.FogEnable = !GameVars.DisableFog;

                if (Race.Current.ConfigFile.DepthCueMode == DepthCueMode.Dark)
                {
                    GameVars.FogColor = new Color(0, 0, 0);
                    Engine.Device.RenderState.FogTableMode = FogMode.Linear;
                    Engine.Device.RenderState.FogEnd = GameVars.DrawDistance + 20; // GameVars.DrawDistance - (Race.Current.ConfigFile.FogAmount * 15);
                    Engine.Device.RenderState.FogStart = (1 / Race.Current.ConfigFile.FogAmount) * 100;
                    Engine.Device.RenderState.FogDensity = Race.Current.ConfigFile.FogAmount * 0.0012f;
                    Engine.Device.RenderState.FogColor = GameVars.FogColor;
                }
                else if (Race.Current.ConfigFile.DepthCueMode == DepthCueMode.Fog)
                {
                    GameVars.FogColor = new Color(245, 245, 245);
                    //Engine.Device.RenderState.FogTableMode = FogMode.ExponentSquared;
                    Engine.Device.RenderState.FogDensity = Race.Current.ConfigFile.FogAmount * 0.0015f;
                    Engine.Device.RenderState.FogTableMode = FogMode.Linear;
                    Engine.Device.RenderState.FogEnd = GameVars.DrawDistance + 20; // -(Race.Current.ConfigFile.FogAmount * 15);
                    Engine.Device.RenderState.FogStart = (1 / Race.Current.ConfigFile.FogAmount) * 100;
                    Engine.Device.RenderState.FogColor = GameVars.FogColor;
                }
                else
                {
                    Trace.Assert(false);
                }
                //_effect.FogEnabled = !GameVars.DisableFog;
                //_effect.FogColor = GameVars.FogColor.ToVector3();
                //_effect.FogStart = 200 * (1/Race.Current.ConfigFile.FogAmount);
                //_effect.FogEnd = Engine.DrawDistance * 8 * (1 / Race.Current.ConfigFile.FogAmount);
                _effect.TextureEnabled = true;
                _effect.TexCoordsMultiplier = 1;
                _effect.PreferPerPixelLighting = false;
                
                

                if (GameVars.LightingEnabled)
                {
                    //_effect.PreferPerPixelLighting = false;
                    _effect.LightingEnabled = true;
                    _effect.AmbientLightColor = new Vector3(0.8f);
                    _effect.DirectionalLight0.DiffuseColor = new Vector3(1);
                    
                    Vector3 dir = new Vector3(-1f, 0.9f, -1f);
                    dir.Normalize();
                    _effect.DirectionalLight0.Direction = dir;
                    _effect.DirectionalLight0.Enabled = true;

                    //_effect.DirectionalLight1.DiffuseColor = new Vector3(0.8f);
                    //dir = new Vector3(1f, 1, 1f);
                    //dir.Normalize();
                    //_effect.DirectionalLight1.Direction = dir;
                    //_effect.DirectionalLight1.Enabled = true;


                    //_effect.SpecularColor = new Vector3(0.3f);
                    //_effect.SpecularPower = 100;
                    //_effect.AmbientLightColor = new Vector3(0.65f);
                    //_effect.DirectionalLight1.Enabled = false;
                    //_effect.DirectionalLight2.Enabled = false;
                    //_effect.TextureEnabled = false;
                    //_effect.DirectionalLight0.Enabled = true;

                    //_effect.PreferPerPixelLighting = true;
                }
            }

            _effect.View = Engine.Camera.View;
            _effect.Projection = Engine.Camera.Projection;

            _effect.Begin(SaveStateMode.None);

            return _effect;
        }

        private void TakeScreenshot()
        {
            int count = Directory.GetFiles(StorageContainer.TitleLocation+"\\", "ndump*.bmp").Length + 1;
            string name = "\\ndump" + count.ToString("000") + ".bmp";

            GraphicsDevice device = Engine.Device;
            using (ResolveTexture2D screenshot = new ResolveTexture2D(device, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight, 1, SurfaceFormat.Color))
            {
                device.ResolveBackBuffer(screenshot);
                screenshot.Save(StorageContainer.TitleLocation + name, ImageFileFormat.Bmp);
            }

            //MessageRenderer.Instance.PostHeaderMessage("Screenshot dumped to " + name, 3);
        }
    }
}
