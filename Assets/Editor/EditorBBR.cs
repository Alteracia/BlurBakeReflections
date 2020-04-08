using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace BBR
{
    [CustomEditor(typeof(PlanerBBR))]
    public class BBREditor : Editor
    {
        private PlanerBBR bbr;
        private bool preview;
        private int res_index = 0;
        private int[] res_options = new int[] { 2048, 1024, 512, 256, 128, 64, 32 };
        public override void OnInspectorGUI()
        {
            bbr = (PlanerBBR)target;
            if (!bbr.ChromaKey_Mat)
                bbr.ChromaKey_Mat = AssetDatabase.LoadAssetAtPath("Assets/BlurBakeReflections/ChromaKey.mat", typeof(Material)) as Material;
            if (!bbr.ViewPoint)
                bbr.ViewPoint = Camera.main.transform;
            if (!bbr.Probe_PPP)
                bbr.Probe_PPP = AssetDatabase.LoadAssetAtPath("Assets/BlurBakeReflections/BBR_Probes_PPP.asset", typeof(VolumeProfile)) as VolumeProfile;
            res_index = Array.FindIndex(res_options, r => r == bbr.Resolution);
            bbr.Steps = 3;
            DrawDefaultInspector();
            if (bbr.PreviewHeights != preview)
            {
                bbr.UpdateHeights();
                preview = bbr.PreviewHeights;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Height: from to", bbr.StartHeight.ToString("0.00"));
            var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(bbr.EndHeight.ToString("0.00"), style);
            if (preview)
            {
                bbr.SetUpPreviewHeights();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.MinMaxSlider(ref bbr.StartHeight, ref bbr.EndHeight, 0, bbr.MaxSceneHeight);
            res_index = EditorGUILayout.Popup("Resolution",
                 res_index, res_options.Select(x => x.ToString()).ToArray(), EditorStyles.popup);
            if (res_options[res_index] != bbr.Resolution)
            {
                bbr.Resolution = res_options[res_index];
            }
            if (GUILayout.Button("Bake Textures"))
            {
                if (bbr.BuildProbe())
                {
                    for (int i = 0; i < bbr.Steps; i++)
                    {
                        EditorUtility.DisplayProgressBar("Reflection Probe", "Baking " + (i + 1).ToString() + "/" + bbr.Steps.ToString(), (float)(i + 1) / bbr.Steps);
                        bbr.RenderProbeStep(i);                       
                    }
                    bbr.CombineReflections();
                    SaveReflection(bbr.Probe_Map);
                    bbr.Clean();
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        private void SaveReflection(Texture2D reflection)
        {
            string targetExtension = "exr"; // probe.hdr ? "exr" : "png";
            string targetPath = "Assets/BlurBakeReflections/Probes/" + SceneManager.GetActiveScene().name + "_" + bbr.transform.name + "_" + bbr.ViewPoint.name;
            if (Directory.Exists(targetPath) == false)
                Directory.CreateDirectory(targetPath);
            string fileName = "temp" + "." + targetExtension; // probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection")
            string path = Path.Combine(targetPath, fileName);
            if (File.Exists(path))
                AssetDatabase.DeleteAsset(path);
            EditorUtility.DisplayProgressBar("Reflection Probe", "Saving " + path, 1);
            // Encode texture into EXR           
            var bytes = reflection.EncodeToEXR();
            // Write to disk
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            DestroyImmediate(reflection);
        }
    }


    /*
    private bool BakeCustomReflectionProbe(ReflectionProbe probe, int step, int Steps)
    {
        string targetExtension = probe.hdr ? "exr" : "png";
        string targetPath = "Assets/BlurBakeReflections/Probes/" + SceneManager.GetActiveScene().name + "_" + bbr.transform.name + "_" + bbr.ViewPoint.name;
        if (Directory.Exists(targetPath) == false)
            Directory.CreateDirectory(targetPath);
        string fileName = probe.name + step.ToString() + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
        string path = Path.Combine(targetPath, fileName);
        if (string.IsNullOrEmpty(path))
            return false;
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
        EditorUtility.DisplayProgressBar("Reflection Probe " + (step + 1).ToString() + "/" + Steps.ToString(), "Baking " + path, (float)(step + 1) / Steps);

        if (!Lightmapping.BakeReflectionProbe(probe, path))
        {
            Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
            return false;
        }

        EditorUtility.ClearProgressBar();
        return true;
    }*/
}
