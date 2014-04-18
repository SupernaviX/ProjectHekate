﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectHekate.Core
{
    public interface IEngine
    {
        IBulletSystem BulletSystem { get; }

        ControllerBuilder CreateController(float x, float y, float angle, bool enabled);
        void Update(float dt);
    }
    
    public class Engine : IEngine
    {
        private readonly BulletSystem _bulletSystem;
        private readonly InterpolationSystem _interpolationSystem;
        private readonly List<Controller> _controllers;

        private readonly List<Emitter> _emitters;

        public IBulletSystem BulletSystem
        {
            get { return _bulletSystem; }
        }

        public Engine()
        {
            _bulletSystem = new BulletSystem();
            _interpolationSystem = new InterpolationSystem();
            _controllers = new List<Controller>();
            _emitters = new List<Emitter>();
        }

        public ControllerBuilder CreateController(float x, float y, float angle, bool enabled)
        {
            return new ControllerBuilder(x, y, angle, enabled, this);
        }

        internal void AddController(Controller con)
        {
            _controllers.Add(con);
        }

        internal void AddEmitters(IEnumerable<Emitter> e)
        {
            _emitters.AddRange(e);
        }
        
        public void Update(float dt)
        {
            // updates all systems
            UpdateControllers();

            _bulletSystem.Update(dt, _interpolationSystem);
            _interpolationSystem.Update();
        }

        private void UpdateControllers()
        {
            foreach (var controller in _controllers) {
                if (controller.Enabled) {
                    UpdateControllersEmitters(controller);
                }
            }
        }

        private void UpdateControllersEmitters(Controller cont)
        {
            foreach(var emitter in cont.Emitters)
            {
                if (!emitter.Enabled)
                {
                    continue;
                }

                while (emitter.Angle > Helpers.Math.TwoPi) emitter.Angle -= Helpers.Math.TwoPi;
                while (emitter.Angle < -Helpers.Math.TwoPi) emitter.Angle += Helpers.Math.TwoPi;

                if (emitter.Orbiting) {
                    // use angle + distance to determine position
                    emitter.X = cont.X + (float) Math.Cos(emitter.Angle)*emitter.OrbitDistance;
                    emitter.Y = cont.Y + (float) Math.Sin(emitter.Angle)*emitter.OrbitDistance;
                }
                else {
                    emitter.X = cont.X + emitter.OffsetX;
                    emitter.Y = cont.Y + emitter.OffsetY;
                }

                // if the emitter does not have a special update function, skip the wait logic
                if (emitter.UpdateFunc == null) {
                    emitter.FramesAlive++;
                    continue;
                }

                // if the emitter's "update state" is ready
                if (emitter.WaitTimer <= 0)
                {
                    var loopAgain = true;

                    // this loopAgain variable basically means: start from the beginning of the Update function if the function reaches completion
                    // this means that if there isn't a yield return, the function will loop infinitely
                    // TODO: somehow prevent that
                    while (loopAgain)
                    {
                        emitter.WaitEnumerator = emitter.WaitEnumerator ?? emitter.Update(_bulletSystem, _interpolationSystem);

                        // this steps through the emitters update function until it hits a yield return
                        if (emitter.WaitEnumerator.MoveNext())
                        {
                            // TODO: check the type of emitter.WaitEnumerator.Current to make sure it isn't null?
                            // starting next frame, this emitter is 'waiting'
                            emitter.WaitTimer = emitter.WaitEnumerator.Current.Delay;

                            loopAgain = false;
                        }
                        else
                        { // if it returns false, then it has hit the end of the function -- so loop again, from the beginning
                            emitter.WaitEnumerator = emitter.Update(_bulletSystem, _interpolationSystem);

                            loopAgain = true;
                        }
                    }
                }
                else
                { // the emitter is "waiting"
                    emitter.WaitTimer--;
                }

                emitter.FramesAlive++;
            }
        }
    }
}
