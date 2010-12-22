﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace OneAmEngine.Audio
{

    public interface ISound
    {
        int Id { get; set; }
        object Owner { get; set; }
        float Duration { get; }
        float Volume { get; set; }
        void Pause();
        void Stop();
        void Reset();
        void Play(bool loop);
        Vector3 Position { get; set; }
        Vector3 Velocity { set; }
        int Frequency { set; }
        bool IsPlaying { get; }
        float MinimumDistance { get; set; }
        float MaximumDistance { get; set; }
        bool MuteAtMaximumDistance { get; set; }
        //    bool 
    }   
}
