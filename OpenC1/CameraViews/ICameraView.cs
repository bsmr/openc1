﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace OpenC1.CameraViews
{
    interface ICameraView
    {

        bool Selectable { get; }
        void Update();
        void Render();
        void Activate();
        void Deactivate();

    }
}
