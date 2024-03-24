using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityModManagerNet;

namespace WheelZoomPC;

[UsedImplicitly]
[EnableReloading]
internal static class Plugin
{
    internal static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
    private static Harmony? Patcher { get; set; }
    
    [UsedImplicitly]
    internal static bool Load(UnityModManager.ModEntry modEntry)
    {
        Logger = modEntry.Logger;
        Patcher = new Harmony(modEntry.Info.Id);
        
        modEntry.OnToggle += OnToggle;
        
        return true;
    }

    private static void Do()
    {
        Patcher?.PatchAll();
    }
    private static void Undo()
    {
        Patcher?.UnpatchAll(Patcher.Id);
    }

    private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
        if (value) Do();
        else Undo();
        return true;
    }
}