
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class AirShipWorldObject : MapParent, ILoadReferenceable
    {
        private Material cachedMat;
        private static readonly Color PlayerCaravanColor = new Color(1f, 0.863f, 0.33f);

        public override Material Material
        {
            get
            {
                if ((Object)this.cachedMat == (Object)null)
                {
                    Color color = this.Faction != null ? (!this.Faction.IsPlayer ? this.Faction.Color : PlayerCaravanColor) : Color.white;
                    this.cachedMat = MaterialPool.MatFrom(this.def.texture, ShaderDatabase.WorldOverlayTransparentLit, color, WorldMaterials.DynamicObjectRenderQueue);
                }
                return this.cachedMat;
            }
        }

        public override string Label => "Unamed ship";
    }
}
