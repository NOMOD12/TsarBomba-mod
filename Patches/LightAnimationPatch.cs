using HarmonyLib;
using UnityEngine;

namespace TSARBOMBA.Patches
{
    /*
     It seems that you have deleted the original OnEnable method.
     I'm not sure about weather it's intended or not, but a example of deleting OnEnable is provided below. Please review and adjust as necessary.

    [HarmonyPatch(typeof(LightAnimation), "OnEnable")]
    public static class LightAnimation_OnEnable_Patch
    {
        static bool Prefix()
        {
            // Return false to skip the original OnEnable method, effectively deleting it.
            return false;
        }
    }

     */

    [HarmonyPatch(typeof(LightAnimation), "Update")]
    public static class LightAnimation_Update_Patch
    {
        static bool Prefix(LightAnimation __instance)
        {
            float num = 5000f;
            __instance.timeSinceSpawn += Time.deltaTime * 0.16f;
            float targetIntensity =
                __instance.lightIntensity
                * __instance.intensityCurve.Evaluate(__instance.timeSinceSpawn)
                * num;
            __instance.animatedLight.intensity =
                Mathf.Lerp(
                    __instance.animatedLight.intensity,
                    targetIntensity,
                    Time.deltaTime * 1f
                );
            __instance.animatedLight.color =
                __instance.colorAnimation.Evaluate(__instance.timeSinceSpawn);

            if (__instance.lightAnchor != null)
            {
                __instance.animatedLight.transform.position =
                    __instance.lightAnchor.position + Vector3.up * __instance.verticalOffset;
            }

            if (__instance.timeSinceSpawn > 2f &&
                __instance.animatedLight.intensity < 0.05f)
            {
                Object.Destroy(__instance.animatedLight.gameObject);
                Object.Destroy(__instance);
            }

            return false;
        }
    }

}