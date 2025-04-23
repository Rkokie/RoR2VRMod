using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace VRPatcher
{
    public static class VRDependenciesPatcher
    {
        private static string VR_MANIFEST = "{ \"name\": \"OpenXR XR Plugin\", \"version\": \"1.13.0\", \"libraryName\": \"UnityOpenXR\", \"displays\": [{\"id\": \"OpenXR Display\" }], \"inputs\": [{\"id\": \"OpenXR Input\"}]} ";
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("VRDependenciesPatcher");

        public static void Initialize()
        {
            Logger.LogInfo("Setting up VR runtime assets");
        
            SetupRuntimeAssets();
        
            Logger.LogInfo("We're done here. Goodbye!");
        }

        /// <summary>
        /// Place required runtime libraries and configuration in the game files to allow VR to be started
        /// </summary>
        public static void SetupRuntimeAssets()
        {
            var root = Path.Combine(Paths.GameRootPath, "Risk of Rain 2_Data");
            var subsystems = Path.Combine(root, "UnitySubsystems");
            if (!Directory.Exists(subsystems))
                Directory.CreateDirectory(subsystems);

            var openXr = Path.Combine(subsystems, "UnityOpenXR");
            if (!Directory.Exists(openXr))
                Directory.CreateDirectory(openXr);

            var manifest = Path.Combine(openXr, "UnitySubsystemsManifest.json");
            if (!File.Exists(manifest))
                File.WriteAllText(manifest, VR_MANIFEST);

            var plugins = Path.Combine(root, "Plugins");
            var oxrPluginTarget = Path.Combine(plugins, "UnityOpenXR.dll");
            var oxrLoaderTarget = Path.Combine(plugins, "openxr_loader.dll");

            var current = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (current == null)
            {
                Logger.LogError(" Did not find current executing assembly location.");
                return;
            } 
            var oxrPlugin = Path.Combine(current, "RuntimeDeps/UnityOpenXR.dll");
            var oxrLoader = Path.Combine(current, "RuntimeDeps/openxr_loader.dll");

            if (!CopyResourceFile(oxrPlugin, oxrPluginTarget))
                Logger.LogWarning("Could not find plugin UnityOpenXR.dll, VR might not work!");

            if (!CopyResourceFile(oxrLoader, oxrLoaderTarget))
                Logger.LogWarning("Could not find plugin openxr_loader.dll, VR might not work!");
        }
        
        public static byte[] ComputeHash(byte[] input)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(input);
            }
        }
    
        /// <summary>
        /// Helper function for SetupRuntimeAssets() to copy resource files and return false if the source does not exist
        /// </summary>
        private static bool CopyResourceFile(string sourceFile, string destinationFile)
        {
            if (!File.Exists(sourceFile))
                return false;

            if (File.Exists(destinationFile))
            {
                var sourceHash = ComputeHash(File.ReadAllBytes(sourceFile));
                var destHash = ComputeHash(File.ReadAllBytes(destinationFile));

                if (sourceHash.SequenceEqual(destHash))
                    return true;
            }

            File.Copy(sourceFile, destinationFile, true);

            return true;
        }

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a list of managed assemblies to patch as a public static <see cref="IEnumerable{T}"/> property named TargetDLLs
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a public static void method named Patch which receives an <see cref="AssemblyDefinition"/> argument,
        /// which patches each of the target assemblies in the TargetDLLs list.
        /// 
        /// We don't actually need to patch any of the managed assemblies, so we are providing an empty method here.
        /// </summary>
        /// <param name="ad"></param>
        [Obsolete("Should not be used!", true)]
        public static void Patch(AssemblyDefinition ad) { }
    }
}
