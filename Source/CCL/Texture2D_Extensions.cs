using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace ResearchEngine
{
    public static class Texture2D_Extensions
    {
        /// </summary>
        /// <param name="tex"></param>
        /// <returns></returns>
        public static Texture2D Crop(this Texture2D tex)
        {
            // see note above
            return tex;
        }

        public static void DrawFittedIn(this Texture2D tex, Rect rect)
        {
            float rectProportion = (float)rect.width / (float)rect.height;
            float texProportion = (float)tex.width / (float)tex.height;

            if (texProportion > rectProportion)
            {
                Rect wider = new Rect(rect.xMin, 0f, rect.width, rect.width / texProportion).CenteredOnYIn(rect).CenteredOnXIn(rect);
                GUI.DrawTexture(wider, tex);
                return;
            }
            else if (texProportion < rectProportion)
            {
                Rect taller = new Rect(0f, rect.yMin, rect.height * texProportion, rect.height).CenteredOnXIn(rect).CenteredOnXIn(rect);
                GUI.DrawTexture(taller, tex);
                return;
            }
            GUI.DrawTexture(rect, tex);
        }
    }
}
