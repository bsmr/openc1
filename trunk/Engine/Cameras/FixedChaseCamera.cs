﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace OneAmEngine
{
    /// <summary>
    /// Camera that stays a fixed distance behind an object but swings freely
    /// </summary>
    public class FixedChaseCamera : ICamera
    {
        public Vector3 ChaseDistance;
        float _currentRotation;
        public float HeightOverride;
        float _height;

        public FixedChaseCamera(float chaseDistance, float height)
		{
            ChaseDistance = new Vector3(chaseDistance, 1, chaseDistance);
            _height = height;
            AspectRatio = Engine.AspectRatio;
            FieldOfView = MathHelper.ToRadians(45f);
            NearPlaneDistance = 1.0f;
            View = Matrix.CreateLookAt(Vector3.One, Vector3.UnitZ, Vector3.Up);
		}

        AverageValueVector3 _lookAt = new AverageValueVector3(45);
		
		/// <summary>
		/// Position of camera in world space.
		/// </summary>
		public Vector3 Position {get; set; }

        public Vector3 Orientation {get; set; }

		/// <summary>
		/// Perspective aspect ratio. Default value should be overriden by application.
		/// </summary>
		public float AspectRatio {get; set; }
		
		/// <summary>
		/// Perspective field of view.
		/// </summary>
        public float FieldOfView { get; set; }

		/// <summary>
		/// Distance to the near clipping plane.
		/// </summary>
		public float NearPlaneDistance {get; set; }

		/// <summary>
		/// Distance to the far clipping plane.
		/// </summary>
		public float DrawDistance {get; set; }		

		/// <summary>
		/// View transform matrix.
		/// </summary>
        public Matrix View { get; private set; }

		/// <summary>
		/// Projecton transform matrix.
		/// </summary>
		public Matrix Projection {get; private set; }

        /// <summary>
        /// Rotation around the target
        /// </summary>
        float _requestedRotation;
        public float RotationSpeed = 3;

        public float Rotation
        {
            get { return _currentRotation; }
        }

        public void RotateTo(float radians)
        {
            _requestedRotation = radians;
        }

        public void ResetRotation()
        {
            _requestedRotation = 0;
            _currentRotation = 0;
        }
		
        public void Update()
		{
            if (_currentRotation != _requestedRotation)
            {
                if (_currentRotation < _requestedRotation)
                    _currentRotation += Engine.ElapsedSeconds * RotationSpeed;
                else
                    _currentRotation -= Engine.ElapsedSeconds * RotationSpeed;
                if (Math.Abs(_currentRotation - _requestedRotation) < 0.01f)
                    _currentRotation = _requestedRotation;
            }

            Vector3 pos = (-Vector3.Normalize(Orientation) * ChaseDistance);
            if (HeightOverride != 0)
                pos.Y = HeightOverride;
            else
                pos.Y += _height;
            _lookAt.AddValue(pos);
            Vector3 avgLookAt = _lookAt.GetAveragedValue();
            Vector3 cameraPosition = Position + Vector3.Transform(avgLookAt, Matrix.CreateRotationY(_currentRotation));
            View = Matrix.CreateLookAt(cameraPosition, Position + new Vector3(0, 1.3f, 0), Vector3.Up);
            Projection = Matrix.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlaneDistance, DrawDistance);
            Position = cameraPosition;
		}
    }
}
