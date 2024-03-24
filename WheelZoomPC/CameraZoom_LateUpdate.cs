using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace WheelZoomPC;

[HarmonyPatch(typeof(CameraZoom), nameof(CameraZoom.LateUpdate))]
[UsedImplicitly]
public class CameraZoomLateUpdate
{
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(MethodBase original,
        IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var getterFieldOfView = typeof(Camera).GetProperty(nameof(Camera.fieldOfView))?.GetGetMethod();

        var finalInstructions = instructions.ToArray();
        for (var currentIdx = 0; currentIdx < finalInstructions.Length; currentIdx++)
        {
            var current = finalInstructions[currentIdx];
            var last = currentIdx > 0 ? finalInstructions[currentIdx - 1] : default;
            var twoBeforeLast = currentIdx > 2 ? finalInstructions[currentIdx - 3] : default;

            if ((current.opcode == OpCodes.Ble_Un || current.opcode == OpCodes.Bge_Un) &&
                last != null && last.opcode == OpCodes.Ldfld &&
                twoBeforeLast != null && twoBeforeLast.Calls(getterFieldOfView))
            {
                yield return new CodeInstruction(OpCodes.Sub);
                yield return new CodeInstruction(OpCodes.Call,
                    typeof(Mathf).GetMethod(nameof(Mathf.Abs), BindingFlags.Public | BindingFlags.Static, null, default,
                        new[] { typeof(float) }, null));
                yield return new CodeInstruction(OpCodes.Ldc_R4, 0.01f);
                yield return new CodeInstruction(OpCodes.Ble_Un, current.operand);
                continue;
            }

            yield return current;
        }
    }

    private static CustomFirstPersonController? cachedController;
    internal static CustomFirstPersonController? CachedController
    {
        get
        {
            if (!cachedController) cachedController = Object.FindObjectOfType<CustomFirstPersonController>();
            return cachedController;
        }
    }
    [UsedImplicitly]
    public static void Prefix(CameraZoom __instance)
    {
        if (KeyBindings.zoomKeys.IsPressed() && !__instance.disableZoomForced)
        {
            if (KeyBindings.GetScrolling() != 0)
                __instance.zoomedFOV = Mathf.Clamp(__instance.zoomedFOV - KeyBindings.GetScrolling() * 5f, 5f,
                    __instance.normalFOV - 5f);
                
            if (!__instance.isZoomPressed || KeyBindings.GetScrolling() != 0)
                SetSensMultiplier(Mathf.Lerp(0.1f, 1f,
                    Mathf.InverseLerp(5f, __instance.normalFOV - 5f, __instance.zoomedFOV)));
        }
            
        if (__instance.isZoomPressed && 
            (!KeyBindings.zoomKeys.IsPressed() || __instance.disableZoomForced))
            SetSensMultiplier(1f);

        return;
        void SetSensMultiplier(float mul)
        {
            if (CachedController != null && CachedController)
                CachedController.m_MouseLook.sensitivityMultiplier = mul;
        }
    }
}