﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectHekate.Core.MathExtras;

namespace ProjectHekate.Core
{
    public delegate IEnumerator<WaitInFrames> ProjectileUpdateDelegate<in TProjectileType>(TProjectileType ap) where TProjectileType : AbstractProjectile;

    public interface IBullet
    {
        float X { get; }

        float Y { get; }

        float Angle { get; }

        /// <summary>
        /// The speed of the bullet (measured in pixels per frame).
        /// </summary>
        float Speed { get; }

        int SpriteIndex { get; }

        bool IsActive { get; }

        uint FramesAlive { get; }

        float Radius { get; }
    }

    public interface ICurvedLaser : IBullet
    {
        uint Lifetime { get; }

        /// <summary>
        /// The list of coordinates of the curved laser.
        /// </summary>
        IReadOnlyCollection<Vector<float>> Coordinates { get; set; }
    }

    public interface IBeam : IBullet
    {
        /// <summary>
        /// The delay before the beam shows up immediately in full width.
        /// </summary>
        uint DelayInFrames { get; }

        /// <summary>
        /// The total lifetime of the beam INCLUDING the startup delay.
        /// </summary>
        uint Lifetime { get; }
    }

    public abstract class AbstractProjectile
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Angle { get; set; }
        public float Speed { get; set; }
        public int SpriteIndex { get; set; }
        public uint FramesAlive { get; set; }
        public float Radius { get; set; }


        public bool IsActive { get { return SpriteIndex >= 0; } }

        internal AbstractProjectile()
        {

            SpriteIndex = -1;
        }
    }
    
    public class Bullet : AbstractProjectile, IBullet
    {
        internal IEnumerator<WaitInFrames> Update()
        {
            return UpdateFunc != null ? UpdateFunc(this) : null;
        }

        virtual internal ProjectileUpdateDelegate<Bullet> UpdateFunc { get; set; }
    }

    public class CurvedLaser : AbstractProjectile, ICurvedLaser
    {
        internal const uint MaxLifetime = 64;
        private uint _lifetime;

        /// <summary>
        /// The lifetime of the laser.
        /// </summary>
        public uint Lifetime
        {
            get { return _lifetime; }
            internal set { _lifetime = System.Math.Max(2, System.Math.Min(value, MaxLifetime)); }
        }

        public IReadOnlyCollection<Vector<float>> Coordinates { get; set; }

        /// <summary>
        /// The list of coordinates of the curved laser.
        /// </summary>
        internal Vector<float>[] InternalCoordinates = new Vector<float>[MaxLifetime];

        public CurvedLaser()
        {
            Coordinates = new ReadOnlyCollection<Vector<float>>(InternalCoordinates);
        }

        internal IEnumerator<WaitInFrames> Update()
        {
            return UpdateFunc != null ? UpdateFunc(this) : null;
        }

        internal ProjectileUpdateDelegate<CurvedLaser> UpdateFunc { get; set; }
    }


        internal IEnumerator<WaitInFrames> Update()
        {
            return UpdateFunc != null ? UpdateFunc(this) : null;
        }

        internal BulletUpdateDelegate UpdateFunc { get; set; }
    }
}