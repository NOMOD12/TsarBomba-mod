using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Shockwave;

namespace TSARBOMBA.Patches
{
    [HarmonyPatch(typeof(Shockwave))]
    public static class Shockwave_Patch
    {
        [HarmonyPrefix]
        [HarmonyPatch("Start")]
        static bool Start_Prefix(Shockwave __instance)
        {
            __instance.dustOpacity = 1f;
            __instance.blastPower = Mathf.Pow(__instance.yieldKilotons * 1000000f, 0.3333f);
            __instance.blastRadius = __instance.blastPower * 108f;
            __instance.blastPropagation = __instance.blastPower * 0.5f;

            if (__instance.vaporCloud != null)
            {
                __instance.vaporCloudMat = __instance.vaporCloud.GetComponent<Renderer>().material;
            }

            if (__instance.groundDecal != null)
            {
                if (Physics.Linecast(__instance.transform.position + __instance.blastRadius * 0.5f * Vector3.up,
                                     __instance.transform.position - __instance.blastRadius * 0.5f * Vector3.up,
                                     out var hitInfo, 64))
                {
                    __instance.groundDecal.transform.SetParent(Datum.origin);
                    __instance.groundDecal.transform.rotation = Quaternion.LookRotation(Vector3.down);
                    __instance.groundDecal.transform.position = hitInfo.point;

                    __instance.decalProjector = __instance.groundDecal.GetComponent<DecalProjector>();
                    __instance.decalProjector.size = new Vector3(__instance.blastRadius * 2f, __instance.blastRadius * 2f, __instance.blastRadius * 0.3f);
                    __instance.decalProjector.material = new Material(__instance.decalProjector.material);
                    __instance.decalProjector.material.SetFloat(Shockwave.id_decalSize, __instance.blastRadius * 2f);
                    __instance.decalProjector.material.SetFloat(Shockwave.id_opacity, 1f);
                }
                else
                {
                    Object.Destroy(__instance.groundDecal);
                }
            }

            if (__instance.waterDecal != null && __instance.transform.position.y < Datum.LocalSeaY + __instance.blastRadius * 0.5f)
            {
                __instance.waterDecal.SetActive(true);
            }

            if (__instance.yieldKilotons < 0.0002f)
            {
                return false;
            }

            Collider[] array = Physics.OverlapSphere(__instance.transform.position, __instance.blastRadius * 2f);
            foreach (var collider in array)
            {
                InfluencedObject item = new InfluencedObject(collider);
                if (item.IsInteractable())
                    __instance.influencedObjects.Add(item);
            }

            if (__instance.yieldKilotons > 0.01)
            {
                TerrainScatter.i.ClearScatters(__instance.transform.GlobalPosition(), __instance.blastRadius * 0.5f);
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        static bool Update_Prefix(Shockwave __instance)
        {
            __instance.blastPropagation += 340f * Time.deltaTime;
            __instance.blastTime += Time.deltaTime;

            if (__instance.groundDecal != null && __instance.decalProjector != null)
            {
                __instance.decalProjector.material.SetFloat(Shockwave.id_shockwaveExpansion, __instance.blastRadius / __instance.blastPropagation);
            }

            if (__instance.blastPropagation > __instance.blastRadius)
            {
                __instance.dustOpacity -= Time.deltaTime * 0.5f;
                if (__instance.decalProjector != null)
                    __instance.decalProjector.material.SetFloat(Shockwave.id_opacity, __instance.dustOpacity);

                if (__instance.dustOpacity <= 0f)
                {
                    Object.Destroy(__instance.groundDecal);
                    Object.Destroy(__instance);
                }

                if (__instance.vaporCloud != null && __instance.cloudAlpha <= 0f)
                {
                    Object.Destroy(__instance.vaporCloud);
                }
            }

            float num = Mathf.Max(__instance.blastPropagation / __instance.blastPower, 1f);
            float num2 = 90000000f / (num * num * num);

            if (num2 > 0.5f)
            {
                for (int i = __instance.influencedObjects.Count - 1; i >= 0; i--)
                {
                    if (__instance.influencedObjects[i].HasShockwaveReached(__instance.transform.position,
                                                                          __instance.blastPropagation,
                                                                          num2,
                                                                          __instance.yieldKilotons * 1000000f,
                                                                          __instance.blastPower,
                                                                          __instance.ownerID))
                    {
                        __instance.influencedObjects.RemoveAt(i);
                    }
                }
            }
            else
            {
                __instance.influencedObjects.Clear();
            }

            if (__instance.vaporCloud != null)
            {
                float num4 = 6f;
                float num5 = 0.25f;
                float time = __instance.blastTime / num4 / num5;

                __instance.vaporCloud.transform.LookAt(SceneSingleton<CameraStateManager>.i.transform.position);
                __instance.vaporCloud.transform.localScale = Vector3.one * __instance.blastPropagation;
                __instance.cloudAlpha = __instance.vaporCloudAlpha.Evaluate(time);

                float num6 = (__instance.vaporCloudEmissiveLight != null && __instance.vaporCloudEmissiveLight.isActiveAndEnabled)
                    ? __instance.vaporCloudEmissiveLight.intensity * __instance.vaporCloudEmissiveFactor
                    : 0f;

                __instance.vaporCloudMat.SetFloat(Shockwave.id_ShockwaveAlpha, __instance.cloudAlpha);
                if (num6 > 0f)
                    __instance.vaporCloudMat.SetFloat(Shockwave.id_Emission, num6);

                __instance.vaporCloudMat.SetFloat(Shockwave.id_Size, __instance.blastPropagation / __instance.vaporCloudDetailScale);
                __instance.vaporCloudMat.SetFloat(Shockwave.id_ShockwaveSoftness, 4f / __instance.vaporCloud.transform.localScale.x);

                if (__instance.cloudAlpha <= 0f)
                    Object.Destroy(__instance.vaporCloud);
            }

            return false;
        }
    }
}
