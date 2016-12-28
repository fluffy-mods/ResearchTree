using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

using RimWorld;
using UnityEngine;
using Verse;
using ResearchEngine;

namespace ResearchEngine.Controller
{

    public class MainMonoBehaviour : MonoBehaviour
    {
        #region Instance Data

        private static bool initOk;
        private static bool gameValid;

        private static int ticks;

        private List<SubController> UpdateControllers = null;

        #endregion

        #region Preloader

        internal static void PreLoad()
        {
            // This is a pre-start sequence to hook some deeper level functions.
            // These functions can be hooked later but it would be after the sequence
            // of operations which call them is complete.

            bool InjectionsOk = true;

            // Find all sub-controllers
            var subControllerClasses = typeof(SubController).AllSubclasses();
            var subControllerCount = subControllerClasses.Count();
            if (subControllerCount == 0)
            {
                InjectionsOk = false;
            }

            // Create sub-controllers
            if (InjectionsOk)
            {
                var subControllers = new SubController[subControllerCount];
                for (int index = 0; index < subControllerCount; ++index)
                {
                    var subControllerType = subControllerClasses.ElementAt(index);
                    var subController = (SubController)Activator.CreateInstance(subControllerType);
                    if (subController == null)
                    {
                        InjectionsOk = false;
                        break;
                    }
                    else
                    {
                        subControllers[index] = subController;
                    }
                }
                if (InjectionsOk)
                {
                    Controller.Data.SubControllers = subControllers;
                }
            }
            initOk = InjectionsOk;
        }

        #endregion

        #region Mono Callbacks

        public void Start()
        {
            enabled = true;
        }

        public void FixedUpdate()
        {
            ticks++;
            if (
                (!gameValid) ||
                (Current.ProgramState != ProgramState.Playing) ||
                (Find.VisibleMap == null) ||
                (Find.VisibleMap.components == null)
            )
            {
                // Do nothing until the game has fully loaded the map and is ready to play
                return;
            }
            LongEventHandler.ExecuteWhenFinished(UpdateSubControllers);
        }

        public void OnLevelWasLoaded(int level)
        {
            // Enable the frame update when the game and map are valid
            // Level 1 means we're in gameplay.
            // enabled = ( ( gameValid )&&( level == 1 ) ) ? true : false;
        }

        #endregion

        #region Long Event Handlers

        public static void Initialize()
        {
            //enabled = false;
            gameValid = false;

            if (!initOk)
            {
                return;
            }

            var subControllers = Controller.Data.SubControllers.ToList();
            if (subControllers.NullOrEmpty())
            {
                return;
            }
            // Validate all subs-systems
            subControllers.Sort((x, y) => (x.ValidationPriority > y.ValidationPriority) ? -1 : 1);
            foreach (var subsys in subControllers)
            {
                if (subsys.ValidationPriority != SubController.DontProcessThisPhase)
                {
                    if (!subsys.Validate())
                    {
                        return;
                    }
                }
                else
                {
                    subsys.State = SubControllerState.Validated;
                }
            }
            // Initialize all sub-systems
            subControllers.Sort((x, y) => (x.InitializationPriority > y.InitializationPriority) ? -1 : 1);
            foreach (var subsys in subControllers)
            {
                if (subsys.InitializationPriority != SubController.DontProcessThisPhase)
                {
                    if (!subsys.Initialize())
                    {
                        return;
                    }
                }
                else
                {
                    subsys.State = SubControllerState.Ok;
                }
            }
            // Yay!
            gameValid = true;
            //enabled = true;
            ticks = 0;
        }


        public void UpdateSubControllers()
        {
            if (UpdateControllers == null)
            {
                // Create a list of sub controllers in update order
                UpdateControllers = Controller.Data.SubControllers.ToList();
                UpdateControllers.Sort((x, y) => (x.UpdatePriority > y.UpdatePriority) ? -1 : 1);
            }

            foreach (var subsys in UpdateControllers)
            {
                if (subsys.UpdatePriority != SubController.DontProcessThisPhase)
                {
                    if (
                        (subsys.State == SubControllerState.Ok) &&
                        (subsys.IsHashIntervalTick(ticks))
                    )
                    {
                        if (!subsys.Update())
                        {
                            return;
                        }
                    }
                }
            }

        }
        #endregion
    }
}
