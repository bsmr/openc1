﻿using System;
using System.Collections.Generic;
using System.Text;

namespace OpenC1.Physics
{
    class AutoGearbox : BaseGearbox
    {
        private const float ChangeUpPoint = 0.94f;
        private const float ChangeDownPoint = 0.6f;

        public AutoGearbox(List<float> ratios, float changeTime)
            : base(ratios, changeTime)
        {
        }

        public override void Update(float motorRpmPercent)
        {
            if (_motor.Rpm < 2 && _currentGear == GEAR_NEUTRAL || _currentGear == GEAR_1)
            {
                if (PlayerVehicleController.GearDown)
                {
                    GearDown();
                }
            }
            if (_currentGear == GEAR_REVERSE || _currentGear == GEAR_NEUTRAL)
            {
                if (PlayerVehicleController.GearUp)
                {
                    GearUp();
                }
            }

            if (_currentGear == GEAR_NEUTRAL)
            {
                //GearUp();
            }

            if (!_motor.WheelsSpinning)
            {
                if (_currentGear == GEAR_REVERSE)
                {
                }
                else if (_currentGear == GEAR_NEUTRAL)
                {
                    
                }
                else
                {
                    if (motorRpmPercent > ChangeUpPoint && _currentGear < Ratios.Count - 1)
                        GearUp();
                    if (motorRpmPercent < ChangeDownPoint && CurrentGear > 1 && GearEngaged && !_motor.IsAccelerating && _motor.CanChangeDown)
                        GearDown();
                }
            }

            base.Update(motorRpmPercent);
        }
    }
}
