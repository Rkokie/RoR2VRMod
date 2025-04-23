using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using System.Security;
using System.Security.Permissions;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Uuvr.VrTogglers;
using VRMod.Camera;
using VRMod.Unity;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace VRMod
{
    [BepInPlugin("com.DrBibop.VRMod", "VRMod", "3.0")]
    [BepInDependency("com.Moffein.BanditTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    public class VRMod : BaseUnityPlugin
    {
        internal static ManualLogSource StaticLogger;

        internal static AssetBundle VRAssetBundle;
        
        private VrTogglerManager? _vrTogglerManager;

        private void Awake()
        {
            StaticLogger = Logger;

            VRAssetBundle = AssetBundle.LoadFromMemory(Properties.Resources.vrmodassets);

            PreloadRuntimeDependencies();
            
            
            foreach (var runtimeDetector in OpenXRRuntimeSelector.GenerateRuntimeDetectorList())
            {
                if (runtimeDetector.name.Contains("SteamVR"))
                {
                    OpenXRRuntimeSelector.SetSelectedRuntime(runtimeDetector.jsonPath);
                    Logger.LogInfo("RUNTIME SET TO STEAMVR: " + runtimeDetector.name + ';' +  runtimeDetector.jsonPath);
                    break;
                }
            }
            
            
            ModConfig.Init();
            ActionAddons.Init();
            SettingsAddon.Init();
            UIFixes.Init();
            CameraFixes.Init();
            CutsceneFixes.Init();
            FocusChecker.Init();
            if (ModConfig.InitialMotionControlsValue)
            {
                RoR2.RoR2Application.isModded = true;
                MotionControls.Init();
                MotionControlledAbilities.Init();
                EntityStateAnimationParameter.Init();
            }

            RoR2.RoR2Application.onLoad += () =>
            {
                InitVR();
                RecenterController.Init();
                UIPointer.Init();
                Haptics.HapticsManager.Init();
                RoR2.RoR2Application.onNextUpdate += InitControllers;
            };
        }

        private void InitControllers()
        {
            Controllers.Init();
            ControllerGlyphs.Init();
        }

        private void InitVR()
        {
            _vrTogglerManager = new VrTogglerManager();
            _vrTogglerManager.ToggleVr();
        }

        private bool PreloadRuntimeDependencies()
        {
            try
            {
                var deps = Path.Combine(Path.GetDirectoryName(Info.Location)!, "RuntimeDeps");

                foreach (var file in Directory.GetFiles(deps, "*.dll"))
                {
                    var filename = Path.GetFileName(file);

                    // Ignore known unmanaged libraries
                    if (filename == "UnityOpenXR.dll" || filename == "openxr_loader.dll")
                        continue;

                    VRMod.StaticLogger.LogDebug($"Preloading '{filename}'...");

                    try
                    {
                        Assembly.LoadFile(file);
                    }
                    catch (Exception ex)
                    {
                        VRMod.StaticLogger.LogWarning($"Failed to preload '{filename}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                VRMod.StaticLogger.LogError(
                    $"Unexpected error occured while preloading runtime dependencies (incorrect folder structure?): {ex.Message}");
                return false;
            }

            return true;
        }
    }
}