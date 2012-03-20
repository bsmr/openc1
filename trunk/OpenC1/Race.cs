﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenC1.Parsers;
using OpenC1.Physics;
using StillDesign.PhysX;
using OpenC1.Parsers.Grooves;
using OpenC1.Parsers.Funks;
using OpenC1.Gfx;
using Microsoft.Xna.Framework.Input;
using OpenC1.GameModes;
using OneAmEngine;
using System.IO;

namespace OpenC1
{
    class Race
    {
        CActorHierarchy _actors;
        List<NonCar> _nonCars;
        public RaceTimeController RaceTime;
        SkyBox _skybox;
        public int NextCheckpoint = 0, CurrentLap, NbrOpponents, NbrDeadOpponents, NbrDeadPeds;
        public Vehicle PlayerVehicle;
        public List<Opponent> Opponents = new List<Opponent>();
        public List<IDriver> Drivers = new List<IDriver>(); //opponent + player drivers
        private RaceMap _map;
        public PedestrianController Peds;

        public static Race Current;

        public RaceFile ConfigFile { get; private set; }

        public Race(string filename, string playerVehicleFile)
        {
            Race.Current = this;

            Logger.Log("Starting race " + Path.GetFileName(filename));

            ConfigFile = new RaceFile(filename);

            foreach (string matFileName in ConfigFile.MaterialFiles)
            {
                MatFile matFile = new MatFile(matFileName);
                ResourceCache.Add(matFile);
            }

            foreach (string pixFileName in ConfigFile.PixFiles)
            {
                PixFile pixFile = new PixFile(pixFileName);
                ResourceCache.Add(pixFile);
            }

            if (GameVars.Emulation == EmulationMode.Demo)
                ResourceCache.Add(new CMaterial("drkcurb.mat", 226)); //demo doesn't have this file, I guess the color is hard-coded
            else
                ResourceCache.Add(new MatFile("drkcurb.mat"));
                        
            ResourceCache.ResolveMaterials();

            if (filename.Contains("TESTMAP")) //nasty hack...
                GameVars.Scale.Y *= 0.5f;

            DatFile modelFile = new DatFile(ConfigFile.ModelFile);
            
            ActFile actFile = new ActFile(ConfigFile.ActorFile);
            _actors = actFile.Hierarchy;
            _actors.AttachModels(modelFile.Models);
            _actors.ResolveTransforms(false, ConfigFile.Grooves);

            if (filename.Contains("TESTMAP")) //nasty hack...
                GameVars.Scale.Y *= 2f;

            // link the actors and grooves
            foreach (BaseGroove g in ConfigFile.Grooves)
                g.SetActor(_actors.GetByName(g.ActorName));

            // link the funks and materials
            foreach (BaseFunk f in ConfigFile.Funks)
            {
                f.Resolve();
            }

            if (ConfigFile.SkyboxTexture != "none")
            {
                PixFile horizonPix = new PixFile(ConfigFile.SkyboxTexture);
                _skybox = SkyboxGenerator.Generate(horizonPix.PixMaps[0].Texture, ConfigFile.SkyboxRepetitionsX - 3f, ConfigFile.DepthCueMode);
                _skybox.HeightOffset = -220 + ConfigFile.SkyboxPositionY * 1.5f;
            }

            Physics.TrackProcessor.GenerateTrackActor(ConfigFile, _actors, out _nonCars);

			Logger.Log("NonCars: " + _nonCars.Count);

            GridPlacer.Reset();

			List<int> opponentIds = new List<int>();
			List<int> pickedNbrs = new List<int>();
			for (int i = 0; i < 5; i++)
			{
				int index = 0;
				while (true)
				{
					index = Engine.Random.Next(1, OpponentsFile.Instance.Opponents.Count);
					if (!pickedNbrs.Contains(index))
					{
						pickedNbrs.Add(index);
						break;
					}
				}
				try
				{
					Opponents.Add(new Opponent(OpponentsFile.Instance.Opponents[index].FileName, ConfigFile.GridPosition, ConfigFile.GridDirection));
					NbrOpponents++;
				}
				catch(Exception ex)
				{
					Logger.Log("Error while loading opponent " + OpponentsFile.Instance.Opponents[index].FileName + ", " + ex.Message);
				}
			}
			
            foreach (CopStartPoint point in ConfigFile.CopStartPoints)
            {
                Opponents.Add(new Opponent(point.IsSpecialForces ? "bigapc.txt" : "apc.txt", point.Position, 0, new CopDriver()));
            }

            foreach (Opponent o in Opponents) Drivers.Add(o.Driver);

            OpponentController.Nodes = ConfigFile.OpponentPathNodes;

            PlayerVehicle = new Vehicle(GameVars.BasePath + @"cars\" + playerVehicleFile, new PlayerDriver());
            PlayerVehicle.PlaceOnGrid(ConfigFile.GridPosition, ConfigFile.GridDirection);
            Drivers.Add(PlayerVehicle.Driver);

            Peds = new PedestrianController(ConfigFile.Peds);
            _map = new RaceMap(this);

            RaceTime = new RaceTimeController();

            PhysX.Instance.Scene.SetActorGroupPairFlags(PhysXConsts.TrackId, PhysXConsts.VehicleId, ContactPairFlag.Forces | ContactPairFlag.OnStartTouch | ContactPairFlag.OnTouch);
            PhysX.Instance.Scene.SetActorGroupPairFlags(PhysXConsts.VehicleId, PhysXConsts.NonCarId, ContactPairFlag.Forces | ContactPairFlag.OnStartTouch | ContactPairFlag.OnTouch);
            PhysX.Instance.Scene.SetActorGroupPairFlags(PhysXConsts.TrackId, PhysXConsts.NonCarId, ContactPairFlag.OnTouch);
            PhysX.Instance.Scene.SetActorGroupPairFlags(PhysXConsts.VehicleId, PhysXConsts.VehicleId, ContactPairFlag.Forces | ContactPairFlag.OnTouch | ContactPairFlag.OnStartTouch | ContactPairFlag.OnEndTouch);
        }


        public void Update()
        {
            RaceTime.Update();

            if (!RaceTime.IsStarted)
            {
                if ((int)RaceTime.TotalTime == 2 && !RaceTime.CountingDown)
                {
                    RaceTime.StartCountdown();
                    foreach (IDriver driver in Drivers)
                        driver.Vehicle.Chassis.FixSuspension();
                }
                if (Engine.Camera is FixedChaseCamera)
                {
                    float height = 55 - (RaceTime.CountdownTime * 35f);
                    ((FixedChaseCamera)Engine.Camera).MinHeight = Math.Max(0, height);
                }
                var closestPath = OpponentController.GetClosestPath(ConfigFile.GridPosition);
                
                foreach (IDriver driver in Drivers)
                    if (driver is CpuDriver)
                    {
                        if (closestPath == null)
                            ((CpuDriver)driver).SetState(CpuDriverState.Sleeping);
                        else
                            ((CpuDriver)driver).TargetNode(closestPath.End);
                    }
                PlayerVehicle.Audio.Play();
            }

            Peds.Update();
            

            foreach (IDriver driver in Drivers)
                driver.Update();

            foreach (BaseGroove groove in ConfigFile.Grooves)
            {
                groove.Update();
            }
            foreach (BaseFunk funk in ConfigFile.Funks)
            {
                funk.Update();
            }

            foreach (NonCar nonCar in _nonCars)
            {
                if (nonCar.CActor.PhysXActor != null && !nonCar.CActor.PhysXActor.IsSleeping && nonCar.CActor.PhysXActor.LinearVelocity.Length() > 1)
                {
                    _actors.RecalculateActorParent(nonCar.CActor);
                }
                if (nonCar.Hit)
                {
                    nonCar.OnHit();
                }
            }

            PlayerVehicle.Update();

            foreach (Opponent opponent in Opponents)
            {
                opponent.Vehicle.Update();
            }

            MessageRenderer.Instance.Update();

            if (Engine.Input.WasPressed(Keys.Tab))
                _map.Show = !_map.Show;

			if (Engine.Input.WasPressed(Keys.T))
				RaceTime.TimeRemaining += 60;
        }

        public void Render()
        {
            if (_skybox != null) _skybox.Draw();

            BoundingFrustum frustum = new BoundingFrustum(Engine.Camera.View * Engine.Camera.Projection);   
            _actors.Render(Matrix.Identity, frustum);

            foreach (Opponent opponent in Opponents)
            {
                if (frustum.Intersects(opponent.GetBoundingSphere()))
                {
                    opponent.Driver.InPlayersView = true;
                    opponent.Vehicle.Render();
                }
                else
                {
                    opponent.Vehicle.SkidMarkBuffer.Render();  //always render skids
                    opponent.Driver.InPlayersView = false;
                }
                opponent.Driver.DistanceFromPlayer = Vector3.Distance(PlayerVehicle.Position, opponent.Vehicle.Position);
            }

            Peds.Render();

            RaceTime.Render();
            MessageRenderer.Instance.Render();
            //Engine.DebugRenderer.AddAxis(Matrix.CreateTranslation(ConfigFile.GridPosition), 10);
            
            if (_map.Show)
            {
                _map.Render();
                return;
            }            
        }

        public void OnCheckpointHit(Checkpoint checkpoint)
        {
            if (checkpoint.Number == NextCheckpoint)
            {
                SoundCache.Play(SoundIds.Checkpoint, PlayerVehicle, false);
                NextCheckpoint++;
                if (NextCheckpoint == ConfigFile.Checkpoints.Count)
                {
                    CurrentLap++;
                    NextCheckpoint = 0;
                }
                MessageRenderer.Instance.PostMainMessage("checkpnt.pix", 3, 0.7f, 0.003f, 1.4f);
            }
            else
            {
                SoundCache.Play(SoundIds.WrongCheckpoint, PlayerVehicle, false);
            }
        }

        public void OnVehicleEnterSpecVol(SpecialVolume volume, Vehicle vehicle)
        {
            int currentVolumeId = vehicle.CurrentSpecialVolume.Count == 0 ? -1 : vehicle.CurrentSpecialVolume.Peek().Id;
            GameConsole.WriteEvent("Enter specvol - " + vehicle.CurrentSpecialVolume.Count);

            if (currentVolumeId != volume.Id)
            {
                volume.Enter(vehicle);
            }
            vehicle.CurrentSpecialVolume.Push(volume);
        }

        public void OnVehicleExitSpecVol(SpecialVolume exitedVolume, Vehicle vehicle)
        {
            SpecialVolume vol = vehicle.CurrentSpecialVolume.Pop();
            SpecialVolume nextVol = vehicle.CurrentSpecialVolume.Count == 0 ? null : vehicle.CurrentSpecialVolume.Peek();
            GameConsole.WriteEvent("Exit specvol - " + vehicle.CurrentSpecialVolume.Count);

            if (nextVol == null)
            {
                //reset
                exitedVolume.Reset(vehicle);
                exitedVolume.Exit(vehicle);
            }
            else if (nextVol.Id != vol.Id)
            {
                nextVol.Enter(vehicle);
                exitedVolume.Exit(vehicle);
            }
        }

        internal void OnCarKilled(Vehicle vehicle)
        {
            foreach (Opponent opponent in Opponents)
            {
				if (opponent.Vehicle == vehicle)
				{
					opponent.Kill();
					if (!(opponent.Driver is CopDriver))
						NbrDeadOpponents++;
					break;
				}
            }

            int time = GeneralSettingsFile.Instance.TimePerCarKill[GameVars.SkillLevel];
            RaceTime.TimeRemaining += time;
            MessageRenderer.Instance.PostTimerMessage(time);

            if (NbrDeadOpponents == Opponents.Count)
            {
                GameMode.Current = new RaceCompletedMode(CompletionType.Opponents);
            }
            else
            {
                MessageRenderer.Instance.PostMainMessage("destroy.pix", 3, 0.7f, 0.003f, 1.4f);
            }
        }

        internal void OnPlayerCpuCarHit(float damage)
        {
            int time = (int)(damage * GeneralSettingsFile.Instance.TimePerCarDamage[GameVars.SkillLevel] * 6);
            if (time > 0)
            {
                RaceTime.TimeRemaining += time;
                MessageRenderer.Instance.PostTimerMessage(time);
                MessageRenderer.Instance.PostHeaderMessage(time*14 + " CREDITS", 2);
            }
        }

        public void OnPedestrianHit(Pedestrian ped, Vehicle vehicle)
        {
			vehicle.LastRunOverPedTime = Engine.TotalSeconds;
			if (ped.IsHit)
			{
				SoundCache.Play(SoundIds.PedSquelch, vehicle, true);
				return;
			}

            NbrDeadPeds++;
            ped.OnHit(vehicle);

			if (vehicle == PlayerVehicle)
			{
				int time = GeneralSettingsFile.Instance.TimePerPedKill[GameVars.SkillLevel];
				RaceTime.TimeRemaining += time;
				MessageRenderer.Instance.PostTimerMessage(time);
			}

            if (NbrDeadPeds == Peds.Count)
            {
                GameMode.Current = new RaceCompletedMode(CompletionType.Peds);
                RaceTime.IsOver = true;
            }
        }

		public void ExitAndReturnToMenu()
		{
			ResourceCache.Clear();
			foreach (var d in Drivers)
				d.Vehicle.Audio.Stop();

			ParticleSystem.AllParticleSystems.Clear();
			Race.Current = null;
			PhysX.Instance.Delete();
			var screen = Engine.Screen.Parent;
			if (screen is PlayGameScreen)  //this will be true if called from the pause screen
				screen = screen.Parent;

			Engine.Screen = null;
			GC.Collect();
			Engine.Screen = screen;
		}
    }
}
