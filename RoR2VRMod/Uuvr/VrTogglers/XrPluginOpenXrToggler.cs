﻿using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Uuvr.VrTogglers
{
    public class XrPluginOpenXrToggler: XrPluginToggler
    {
        protected override XRLoader CreateLoader()
        {
            var xrLoader = ScriptableObject.CreateInstance<OpenXRLoader>();
            OpenXRSettings.Instance.renderMode = OpenXRSettings.RenderMode.MultiPass;
            OpenXRSettings.Instance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            return xrLoader;
        }
    }
}