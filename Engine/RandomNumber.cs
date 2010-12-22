﻿using System;
using Microsoft.Xna.Framework;

namespace OneamEngine
{
    public class RandomGenerator
    {
        private Random _random;

        public RandomGenerator()
        {
            _random = new Random();
        }

        public int Next()
        {
            return _random.Next();
        }

        public float Next(float minValue, float maxValue)
        {
            return (float)(minValue + (float)_random.NextDouble() * (maxValue - minValue));
        }

        public Vector3 Next(Vector3 minValue, Vector3 maxValue)
        {
            Vector3 vec = new Vector3();
            vec.X = (float)(minValue.X + (float)_random.NextDouble() * (maxValue.X - minValue.X));
            vec.Y = (float)(minValue.Y + (float)_random.NextDouble() * (maxValue.Y - minValue.Y));
            vec.Z = (float)(minValue.Z + (float)_random.NextDouble() * (maxValue.Z - minValue.Z));
            return vec;
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }
    }
}

