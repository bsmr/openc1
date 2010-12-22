﻿using System;
using System.Collections.Generic;

using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using OpenC1.Parsers.Grooves;
using OpenC1.Parsers.Funks;
using Microsoft.Xna.Framework.Graphics;
using System.Globalization;

namespace OpenC1.Parsers
{
    enum DepthCueMode
    {
        None,
        Dark,
        Fog
    }

    class CopStartPoint
    {
        public Vector3 Position;
        public bool IsSpecialForces;
    }
    
    class RaceFile : BaseTextFile
    {
        private float PEDESTRIAN_AUTO_Y_FLAG = 1000.4f * GameVars.Scale.Y;

        public List<string> MaterialFiles { get; private set; }
        public List<string> PixFiles { get; private set; }
        public string ModelFile { get; private set; }
        public string ActorFile { get; private set; }
        public string AdditionalActorFile { get; private set; }
        public string SkyboxTexture { get; private set; }
        public float SkyboxPositionY, SkyboxRepetitionsX;
        public DepthCueMode DepthCueMode { get; private set; }
        public int[] InitialTimerValues;
        public float FogAmount;
        public Vector3 GridPosition;
        public float GridDirection;
        public List<NoncarFile> NonCars { get; set; }
        public List<CopStartPoint> CopStartPoints { get; set; }
        public List<BaseGroove> Grooves;
        public List<BaseFunk> Funks;
        public List<MaterialModifier> MaterialModifiers;
        public List<Color> SmokeTables;
        public List<Checkpoint> Checkpoints;
        public int LapCount;
        public List<SpecialVolume> SpecialVolumes;
        public List<OpponentPathNode> OpponentPathNodes;
        public string MapTexture;
        public Matrix MapTranslation;
        public List<Pedestrian> Peds;

        int _fileVersion;
        

        public RaceFile(string filename) : base(filename)
        {
            MaterialFiles = new List<string>();
            PixFiles = new List<string>();
                        
            string version = ReadLine();
            _fileVersion = int.Parse(version.Substring(8,1));
            
            GridPosition = ReadLineAsVector3() + new Vector3(0, GameVars.Scale.Y*0.5f, 0);

            GridDirection = MathHelper.ToRadians(ReadLineAsInt());
            InitialTimerValues = ReadLineAsIntList();
            LapCount = ReadLineAsInt();
            SkipLines(3);  //race completed bonuses
            SkipLines(2); //?
            
            ReadCheckpointsSection();

            int nbrPixMaps = ReadLineAsInt();
            for (int i = 0; i < nbrPixMaps; i++)
            {
                PixFiles.Add(ReadLine());
            }
            int nbrPixMapsLowMem = ReadLineAsInt();
            SkipLines(nbrPixMapsLowMem);
            int nbrShadeTabs = ReadLineAsInt();
            SkipLines(nbrShadeTabs);

            int nbrMaterials = ReadLineAsInt();
            for (int i = 0; i < nbrMaterials; i++)
            {
                MaterialFiles.Add(ReadLine());
            }
            int nbrMaterialsLowMem = ReadLineAsInt();
            SkipLines(nbrMaterialsLowMem);

            int nbrModels = ReadLineAsInt();
            ModelFile = ReadLine();

            if (_fileVersion >= 6)
            {
                int nbrLowMemModels = ReadLineAsInt();
                SkipLines(nbrLowMemModels);
            }

            ActorFile = ReadLine();
            if (_fileVersion >= 6)
            {
                SkipLines(1); //low mem actor
                
            }
            if (_fileVersion >= 7)
            {
                SkipLines(1); // default transparency of '!' materials
            }
            
            AdditionalActorFile = ReadLine();

            SkyboxTexture = ReadLine().ToLower();
            SkyboxRepetitionsX = ReadLineAsInt();
            ReadLine();
            SkyboxPositionY = ReadLineAsInt();
            string cueMode = ReadLine().ToLower();
            if (cueMode == "dark") DepthCueMode = DepthCueMode.Dark;
            else if (cueMode == "fog") DepthCueMode = DepthCueMode.Fog;
            else DepthCueMode = DepthCueMode.Fog; //default to fog?
            FogAmount = ReadLineAsFloatList()[0]; //degree of dark

            int defaultEngineNoise = ReadLineAsInt();
            
            ReadSpecialEffectsVolumes();

            SkipLines(3);  //reflective windscreen stuff
            int nbrAlternativeReflections = ReadLineAsInt();
            SkipLines(nbrAlternativeReflections * 2);

            MapTexture = ReadLine();
            MapTranslation = ReadMatrix();

            ReadFunkSection();

            ReadGrooveSection();

            ReadPedestrianSection();

            ReadOpponentPathsSection();

            ReadCopStartPointsSection();

            ReadMaterialModifierSection();

            ReadNonCarSection();

            ReadSmokeTablesSection();
            
            CloseFile();
        }

        private void ReadCheckpointsSection()
        {
            Checkpoints = new List<Checkpoint>();

            int nbrCheckPoints = ReadLineAsInt();
            for (int i = 0; i < nbrCheckPoints; i++)
            {
                SkipLines(2);
                int quads = ReadLineAsInt();
                Trace.Assert(quads == 1);
                Checkpoint point = new Checkpoint { Number = i };
                List<Vector3> points = new List<Vector3>();
                points.Add(ReadLineAsVector3());
                points.Add(ReadLineAsVector3());
                points.Add(ReadLineAsVector3());
                points.Add(ReadLineAsVector3());
                point.BBox = BoundingBox.CreateFromPoints(points);
                Checkpoints.Add(point);
                SkipLines(2);
            }
        }

        private void ReadSpecialEffectsVolumes()
        {
            SpecialVolumes = new List<SpecialVolume>();
            int nbrSpecialEffectsVolumes = ReadLineAsInt();
            for (int i = 0; i < nbrSpecialEffectsVolumes; i++)
            {
                string name = ReadLine();

                SpecialVolume vol = new SpecialVolume();
                vol.Id = i;

                if (name != "DEFAULT WATER")
                {
                    Matrix m = ReadMatrix();
                                        
                    m = GameVars.ScaleMatrix * Matrix.CreateScale(2) * m;
                    m.Translation = GameVars.Scale * m.Translation;
                    vol.Matrix = m;
                }
                vol.Gravity = ReadLineAsFloat(false);
                vol.Viscosity = ReadLineAsFloat(false);
                vol.CarDamagePerMs = ReadLineAsFloat(false);
                vol.PedDamagePerMs = ReadLineAsFloat(false);
                vol.CameraEffectIndex = ReadLineAsInt();
                vol.SkyColor = ReadLineAsInt();
                vol.WindscreenMaterial = ReadLine();
                vol.EntrySoundId = ReadLineAsInt();
                vol.ExitSoundId = ReadLineAsInt();
                vol.EngineSoundIndex = ReadLineAsInt();
                vol.MaterialIndex = ReadLineAsInt();
                SpecialVolumes.Add(vol);
            }
        }

        private void ReadFunkSection()
        {
            Trace.Assert(ReadLine() == "START OF FUNK");
            Funks = new List<BaseFunk>();
            FunkReader reader = new FunkReader();

            while (!reader.AtEnd)
            {
                BaseFunk f = reader.Read(this);
                if (f != null) Funks.Add(f);
            }
        }

        private void ReadGrooveSection()
        {
            string start = ReadLine();
            Trace.Assert(start == "START OF GROOVE");
            Grooves = new List<BaseGroove>();
            GrooveReader reader = new GrooveReader();
            while (!reader.AtEnd)
            {
                BaseGroove g = reader.Read(this);
                if (g != null) Grooves.Add(g);
            }
        }

        private void ReadPedestrianSection()
        {
            Peds = new List<Pedestrian>();

            ReadLine(); //# of pedsubs
            int nbrPeds = ReadLineAsInt();                       

            for (int i = 0; i < nbrPeds; i++)
            {
                Pedestrian ped = new Pedestrian();
                ped.RefNumber = ReadLineAsInt();
                
                int nbrInstructions = ReadLineAsInt();
                ped.InitialInstruction = ReadLineAsInt() -1;  //1-based
                for (int j = 0; j < nbrInstructions; j++)
                {
                    string type = ReadLine();
                    if (type == "point")
                    {
                        PedestrianInstruction instruction = new PedestrianInstruction();
                        instruction.Position = ReadLineAsVector3();
                        if (instruction.Position.Y > 500)
                        {
                            instruction.Position.Y -= PEDESTRIAN_AUTO_Y_FLAG;
                            instruction.AutoY = true;
                        }
                        ped.Instructions.Add(instruction);
                    }
                    else if (type == "reverse") 
                    {
                        ped.Instructions[ped.Instructions.Count - 1].Reverse = true;
                        if (ped.InitialInstruction >= j) ped.InitialInstruction--;
                    }
                    else
                    {
                    }
                }
                Peds.Add(ped);
            }
        }

        private void ReadOpponentPathsSection()
        {
            Trace.Assert(ReadLine() == "START OF OPPONENT PATHS");
            
            int nbrNodes = ReadLineAsInt();

            OpponentPathNodes = new List<OpponentPathNode>();
            
            for (int i = 0; i < nbrNodes; i++)
            {
                OpponentPathNodes.Add(new OpponentPathNode { Position = ReadLineAsVector3(), Number=i });
            }

            int nbrSections = ReadLineAsInt();
            for (int i = 0; i < nbrSections; i++)
            {
                string[] tokens = ReadLine().Split(',');

                OpponentPathNode startNode = OpponentPathNodes[int.Parse(tokens[0])];

                OpponentPath path = new OpponentPath();
                path.Number = i;
                path.Start = startNode;
                path.End = OpponentPathNodes[int.Parse(tokens[1])];
                path.MinSpeedAtEnd = float.Parse(tokens[4]) * 2.2f; //speeds are in BRU (BRender units). Convert to game speed
                path.MaxSpeedAtEnd = float.Parse(tokens[5]) * 2.2f;
                path.Width = float.Parse(tokens[6]) * 6.5f;
                path.Type = (PathType)int.Parse(tokens[7]);

                startNode.Paths.Add(path);
            }
        }

        private void ReadCopStartPointsSection()
        {
            CopStartPoints = new List<CopStartPoint>();
            int nbrPoints = ReadLineAsInt();
            for (int i = 0; i < nbrPoints; i++)
            {
                string[] tokens = ReadLine().Split(',');
                Vector3 pos = new Vector3(float.Parse(tokens[0]), float.Parse(tokens[1]), float.Parse(tokens[2]));
                pos *= GameVars.Scale;
                pos += new Vector3(0, 2, 0);
                CopStartPoints.Add(new CopStartPoint { Position = pos, IsSpecialForces = tokens[3].Contains("9") });
            }
            Trace.Assert(ReadLine() == "END OF OPPONENT PATHS");
        }

        private void ReadMaterialModifierSection()
        {
            MaterialModifiers = new List<MaterialModifier>();

            int nbrMaterialModifiers = ReadLineAsInt();
            for (int i = 0; i < nbrMaterialModifiers; i++)
            {
                MaterialModifier modifier = new MaterialModifier
                    {
                        CarWallFriction = ReadLineAsFloat(false),
                        TyreRoadFriction = Math.Min(1, ReadLineAsFloat(false)),  /* deal with weird (wrong?) settings for grass on cityb maps*/
                        Downforce = ReadLineAsFloat(false),
                        Bumpiness = ReadLineAsFloat(false),
                        TyreSoundIndex = ReadLineAsInt(),
                        CrashSoundIndex = ReadLineAsInt(),
                        ScrapeSoundIndex = ReadLineAsInt(),
                        Sparkiness = ReadLineAsFloat(false),
                        SmokeTableIndex = ReadLineAsInt()
                    };

                string matName = ReadLine();
                if (matName != "none")
                {
                    if (!File.Exists(GameVars.BasePath + "material\\" + matName))
                    {
                        matName = "SKIDMARK.MAT"; //default skidmark if invalid (in indust maps, this is "1" and skidmarks aren't shown)
                    }
                    MatFile matFile = new MatFile(matName);
                    modifier.SkidMaterial = matFile.Materials[0];
                    modifier.SkidMaterial.ResolveTexture();
                }
                MaterialModifiers.Add(modifier);
            }
        }

        private void ReadNonCarSection()
        {
            NonCars = new List<NoncarFile>();

            int nbrNonCars = ReadLineAsInt();
            for (int i = 0; i < nbrNonCars; i++)
            {
                string filename = ReadLine();
                NoncarFile file = new NoncarFile(GameVars.BasePath + "Noncars\\" + filename);
                NonCars.Add(file);
            }
        }

        private void ReadSmokeTablesSection()
        {
            SmokeTables = new List<Color>();
            int nbrSmokeTables = ReadLineAsInt();
            for (int i = 0; i < nbrSmokeTables; i++)
            {
                SmokeTables.Add(ReadLineAsColor());
                ReadLine();  //strengths
            }

            // now we have smoke tables, initialize material modifiers

            foreach (MaterialModifier modifier in MaterialModifiers)
                modifier.Initialize(this);
        }
    }
}
