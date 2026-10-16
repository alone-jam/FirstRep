using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace FirstRep.Fps
{
    public static class PresentationSettings
    {
        public static void Configure(Camera camera)
        {
            var hd = camera.GetComponent<HDAdditionalCameraData>();
            if (!hd) hd = camera.gameObject.AddComponent<HDAdditionalCameraData>();
            hd.volumeLayerMask = ~0;
            hd.customRenderingSettings = true;
            // A single invalid legacy shading pixel otherwise spreads through Bloom.
            // HDRP requires both the camera switch and its frame setting for this pass.
            hd.stopNaNs = true;
            Set(hd, FrameSettingsField.StopNaN, true);
            // Restore the original presentation features. Disable switches are
            // opt-in diagnostic controls, never the normal game configuration.
            var args = Environment.GetCommandLineArgs();
            Set(hd, FrameSettingsField.Postprocess, Array.IndexOf(args, "-firstrep-diag-disable-post") < 0);
            Set(hd, FrameSettingsField.SubsurfaceScattering, Array.IndexOf(args, "-firstrep-diag-disable-sss") < 0);
            Set(hd, FrameSettingsField.ExposureControl, true);
            Set(hd, FrameSettingsField.Tonemapping, true);
            Set(hd, FrameSettingsField.ColorGrading, true);
            if(Array.IndexOf(args,"-firstrep-diag-allow-invalid-colors")>=0)hd.stopNaNs=false;
            if(Array.IndexOf(args,"-firstrep-diag-disable-tonemap")>=0)Set(hd,FrameSettingsField.Tonemapping,false);
            if(Array.IndexOf(args,"-firstrep-diag-disable-grade")>=0)Set(hd,FrameSettingsField.ColorGrading,false);
            if(Array.IndexOf(args,"-firstrep-diag-disable-stop-nan")>=0)Set(hd,FrameSettingsField.StopNaN,false);
            foreach(var field in new[]{FrameSettingsField.MotionBlur,FrameSettingsField.Bloom,FrameSettingsField.DepthOfField,FrameSettingsField.Antialiasing})
                if(Array.IndexOf(args,"-firstrep-diag-disable-"+field.ToString().ToLowerInvariant())>=0)Set(hd,field,false);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var exposure = profile.Add<Exposure>(true);
            // Original PPv2 SceneCam_Profile_Default: histogram 67.541336..99,
            // clamp log2 luminance to 3.5..7, multiplier = 0.5 / luminance.
            // HDRP: EV=log2(L*100/K)-compensation; multiplier=1/(scale*2^EV).
            // Convert the clamped endpoints AFTER compensation (HDRP order).
            float scale = ColorUtils.lensImperfectionExposureScale;
            float calibration = ColorUtils.s_LightMeterCalibrationConstant;
            float compensation = Mathf.Log(0.5f * 100f * scale / calibration, 2f);
            float offset = Mathf.Log(100f / calibration, 2f) - compensation;
            exposure.mode.Override(ExposureMode.AutomaticHistogram);
            exposure.histogramPercentages.Override(new Vector2(67.541336f, 99f));
            exposure.compensation.Override(compensation);
            exposure.limitMin.Override(3.5f + offset);
            exposure.limitMax.Override(7f + offset);
            // PPv2 eyeAdaptation=0 is Progressive (HDRP uses a different enum order).
            exposure.adaptationMode.Override(AdaptationMode.Progressive);
            exposure.adaptationSpeedDarkToLight.Override(5f);
            exposure.adaptationSpeedLightToDark.Override(2.5f);
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var grading = profile.Add<ColorAdjustments>(true);
            grading.saturation.Override(20f);
            grading.contrast.Override(-5f);
            grading.postExposure.Override(0f);
            var volume = camera.gameObject.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 100; volume.profile = profile;
        }
        static void Set(HDAdditionalCameraData hd, FrameSettingsField field, bool enabled)
        {
            hd.renderingPathCustomFrameSettings.SetEnabled(field, enabled);
            hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
        }
    }
}
