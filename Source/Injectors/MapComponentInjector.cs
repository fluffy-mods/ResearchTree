using System;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace ResearchTree       // Replace with yours.
{
    [StaticConstructorOnStartup]
    public class MapComponentInjector : MonoBehaviour
    {
        private static Type queue = typeof(Queue);


        #region No editing required


        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }


            if (Find.VisibleMap.components.FindAll(c => c.GetType() == queue).Count == 0)
            {
                Find.VisibleMap.components.Add((MapComponent)Activator.CreateInstance(queue));

                Log.Message("Research tree : Queue mapcomponent injected.");
            }
            Destroy(this);
        }

    }
}
#endregion