using HarmonyLib;
using UnityEngine;

namespace TSARBOMBA.Patches
{
    [HarmonyPatch(typeof(DynamicMap), "DisplayExclusionZone")]
    public static class DynamicMap_DisplayExclusionZone_Patch
    {
        [HarmonyPrefix]
        static bool DisplayExclusionZone_Prefix(NuclearOption.ExclusionZone exclusionZone)
        {
            GameObject icon = Object.Instantiate(GameAssets.i.exclusionZoneDisplay, DynamicMap.i.iconLayer.transform);

            icon.transform.localPosition = new Vector3(
                exclusionZone.position.x,
                exclusionZone.position.z,
                0f
            ) * DynamicMap.i.mapDisplayFactor;

            icon.transform.localScale = Vector3.one * (exclusionZone.radius * DynamicMap.i.mapDisplayFactor * 6f);

            if (UnitRegistry.TryGetUnit(exclusionZone.sourceId, out Unit host))
            {
                var evtInfo = typeof(Unit).GetEvent("onDisableUnit");
                System.Action<Unit> handler = _ => Object.Destroy(icon);
                evtInfo.AddEventHandler(host, handler);
            }
            else
            {
                Object.Destroy(icon, 30f);
            }
            return false;
        }
    }
}
