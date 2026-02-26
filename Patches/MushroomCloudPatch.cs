using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TSARBOMBA.Patches
{
    //Example to do minor fixes to the original code by transpiling.
    //You can also use Prefix or Postfix, which is simpler and more efficient than transpiling.
    //I will use Prefix/Postfix to do the most works to save time

    [HarmonyPatch(typeof(MushroomCloud.CloudRing), "Update")]
    public static class MushroomCloud_CloudRing_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 50f)
                {
                    codes[i].operand = 75f;
                }
                if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 340f)
                {
                    codes[i].operand = 54f;
                }

                if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo mi &&
                    mi.Name == "op_Multiply" &&
                    i + 1 < codes.Count &&
                    codes[i + 1].opcode == OpCodes.Callvirt) // set_localScale
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, 6f));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Mul));
                    i += 2;
                }

                if (codes[i].opcode == OpCodes.Ldarg_0 &&
                    i + 2 < codes.Count &&
                    codes[i + 1].opcode == OpCodes.Ldfld &&
                    ((FieldInfo)codes[i + 1].operand).Name == "maxRadius" &&
                    codes[i + 2].opcode == OpCodes.Div)
                {
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldc_R4, 1.5f));
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                    i += 2;
                }
            }

            return codes;
        }
    }

    [HarmonyPatch(typeof(MushroomCloud.Fireball), "Update")]
    public static class MushroomCloud_Fireball_Update_Patch
    {
        static bool Prefix(MushroomCloud.Fireball __instance, float time, ref bool __result)
        {
            if (!__instance.renderer.enabled)
            {
                __result = false;
                return false;
            }
            float num = time * 0.2f;
            __instance.renderer.transform.localScale = Vector3.one * __instance.sizeOverTime.Evaluate(num);

            Material material = __instance.renderer.material;
            int nameID = __instance.id_FireballColor;
            var lastTime = __instance.emitOverTime.keys[__instance.emitOverTime.keys.Length - 1].time;
            Color color = __instance.colorOverTime.Evaluate(num / lastTime);
            Color value = new Color(color.r, color.g, color.b, Mathf.Max(color.a, 0.4f));
            material.SetColor(nameID, value);

            float a = __instance.emitOverTime.Evaluate(num);
            float num2 = Mathf.Max(a, 0.3f);
            __instance.renderer.material.SetFloat(__instance.id_EmissiveStrength, num2 * 18f);

            if (num > lastTime)
            {
                __instance.renderer.enabled = false;
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(MushroomCloud.Cloud), "Update")]
    public static class MushroomCloud_Cloud_Update_Patch
    {
        static bool Prefix(MushroomCloud.Cloud __instance, float time)
        {
            if (__instance.underwater)
                return false;

            float scaledTime = time / 6.5f;

            if (__instance.emitOverTime.Enabled)
            {
                float num2 = __instance.emitOverTime.Value.Evaluate(scaledTime);
                __instance.emitOverTime.SetEnabled(num2 > 0f);

                float num3 = 7f;
                float alphaMin = 0.7f;
                float num4 = Mathf.Max(num2, alphaMin);
                __instance.meshRenderer.material.SetFloat(__instance.id_EmissiveStrength, num4 * num3);

                __instance.meshRenderer.material.SetFloat(
                    __instance.id_EmissiveRoughness,
                    __instance.emitRoughnessOverTime.Value.Evaluate(scaledTime)
                );

                Color color = __instance.colorOverTime.Evaluate(scaledTime / __instance.lifetime);
                Color value = new Color(color.r, color.g, color.b, Mathf.Max(color.a, alphaMin));
                __instance.meshRenderer.material.SetColor(__instance.id_CloudColor, value);
            }

            // Scroll
            __instance.meshRenderer.material.SetFloat(
                __instance.id_ScrollPosition,
                __instance.scrollOverTime.Evaluate(scaledTime)
            );

            __instance.meshRenderer.transform.position += (
                Vector3.up * __instance.riseRate +
                NetworkSceneSingleton<LevelInfo>.i.GetWind(__instance.meshRenderer.transform.position.ToGlobalPosition())
            ) * Time.deltaTime;

            float num5 = __instance.horizontalSizeOverTime.Evaluate(scaledTime);
            float num6 = __instance.relativeHeightOverTime.Evaluate(scaledTime);
            float num7 = Mathf.Clamp(1f + scaledTime / 0.5f, 7f, 40f);
            __instance.meshRenderer.transform.localScale = new Vector3(num5 * num7, num5 * num6 * num7, num5 * num7);

            if (!(Time.timeSinceLevelLoad - __instance.lastSlowUpdate < 1f))
            {
                __instance.lastSlowUpdate = Time.timeSinceLevelLoad;
                Color color2 = __instance.colorOverTime.Evaluate(scaledTime / __instance.lifetime);
                __instance.meshRenderer.material.SetColor(__instance.id_CloudColor, color2);

                __instance.riseRate = __instance.riseRateOverTime.Evaluate(scaledTime);
                __instance.appliedWind = NetworkSceneSingleton<LevelInfo>.i.GetWind(
                    __instance.meshRenderer.transform.position.ToGlobalPosition()
                ) * __instance.windInfluenceOverTime.Evaluate(scaledTime);

                __instance.main.startSize = new ParticleSystem.MinMaxCurve(num5 * 5f);
                __instance.emission.rateOverTime = new ParticleSystem.MinMaxCurve(
                    __instance.particleRateOverTime.Evaluate(scaledTime) * 20f
                );
                __instance.main.startColor = new ParticleSystem.MinMaxGradient(color2 * 0.8f, color2 * 1.2f);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(MushroomCloud.BaseCloud), "Update")]
    public static class MushroomCloud_BaseCloud_Update_Patch
    {
        static bool Prefix(MushroomCloud.BaseCloud __instance, float time, ref bool __result)
        {
            if (__instance.radiusOverTime.Enabled)
            {
                float num = time / __instance.lifetime;
                __instance.shape.radius = __instance.radiusOverTime.Value.Evaluate(num) * 7f;

                float num2 = num;
                Keyframe[] keys = __instance.radiusOverTime.Value.keys;
                __instance.radiusOverTime.SetEnabled(num2 < keys[keys.Length - 1].time);
            }

            if (Time.timeSinceLevelLoad - __instance.lastSlowUpdate < 1f)
            {
                __result = true;
                return false;
            }

            __instance.lastSlowUpdate = Time.timeSinceLevelLoad;

            float time2 = time / (__instance.lifetime * Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f));

            Color color = __instance.colorOverTime.Evaluate(time2);
            Color min = new Color(
                color.r * 1.2f, color.g * 1.2f, color.b * 1.2f,
                1.2f * color.a * Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f)
            );
            Color max = new Color(
                color.r * 0.8f, color.g * 0.8f, color.b * 0.8f,
                0.8f * color.a * Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f)
            );
            __instance.main.startColor = new ParticleSystem.MinMaxGradient(min, max);

            if (__instance.startSizeOverTime.Enabled)
            {
                __instance.main.startSize =
                    new ParticleSystem.MinMaxCurve(__instance.startSizeOverTime.Value.Evaluate(time2) * 5f);
            }

            if (__instance.rateOverTime.Enabled)
            {
                __instance.emission.rateOverTime =
                    new ParticleSystem.MinMaxCurve(
                        __instance.groundBurstFactor * __instance.rateOverTime.Value.Evaluate(time2)
                    );
            }

            __result = color.a > 0f;
            return false;
        }
    }

    [HarmonyPatch(typeof(MushroomCloud.Stem), "Update")]
    public static class MushroomCloud_Stem_Update_Patch
    {
        static bool Prefix(MushroomCloud.Stem __instance, float time, ref bool __result)
        {
            if (Time.timeSinceLevelLoad - __instance.lastEmit > __instance.emitInterval)
            {
                __instance.lastEmit = Time.timeSinceLevelLoad;
                ParticleSystem.EmitParams emitParams = default;
                Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
                insideUnitCircle = insideUnitCircle.normalized * insideUnitCircle.sqrMagnitude;
                Vector3 vector = new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y);

                emitParams.position = vector * __instance.blastScale * 600f + Vector3.up * __instance.blastScale * 20f;
                emitParams.velocity = -(vector * __instance.blastScale * 20f) + Vector3.up * 5f;
                __instance.system.Emit(emitParams, 1);
            }

            if (Time.timeSinceLevelLoad - __instance.lastSlowUpdate < 1f)
            {
                __result = true;
                return false;
            }

            __instance.lastSlowUpdate = Time.timeSinceLevelLoad;

            float time2 = time / (__instance.lifetime * __instance.groundBurstFactor);

            Color color = __instance.startColorOverTime.Evaluate(time2);
            Color min = new Color(
                color.r * 1.2f, color.g * 1.2f, color.b * 1.2f,
                1.2f * color.a * Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f)
            );
            Color max = new Color(
                color.r * 0.8f, color.g * 0.8f, color.b * 0.8f,
                0.8f * color.a * Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f)
            );
            __instance.main.startColor = new ParticleSystem.MinMaxGradient(min, max);

            if (__instance.rateOverTime.Enabled)
            {
                __instance.emitInterval = __instance.rateOverTime.Value.Evaluate(time2);
                __instance.emission.rateOverTime =
                    new ParticleSystem.MinMaxCurve(__instance.groundBurstFactor * __instance.emitInterval);
                __instance.emitInterval = 0.5f / __instance.emitInterval;
            }

            if (__instance.startSizeOverTime.Enabled)
            {
                float num = __instance.startSizeOverTime.Value.Evaluate(time2);
                __instance.main.startSize = new ParticleSystem.MinMaxCurve(num * 12.8f, num * 19.2f);
            }

            if (__instance.lifeOverTime.Enabled)
            {
                float num2 = Mathf.Clamp(__instance.groundBurstFactor, 0.5f, 1f) * __instance.lifeOverTime.Value.Evaluate(time);
                __instance.main.startLifetime = new ParticleSystem.MinMaxCurve(num2 * 2.4f, num2 * 3.6f);
            }

            __result = color.a > 0f;
            return false;
        }
    }

}


