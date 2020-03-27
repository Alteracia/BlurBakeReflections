using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(PlanerBBR))]
public class BBREditor : Editor
{
    private int res_index = 0;
    private int[] res_options = new int[] { 2048, 1024, 512, 256, 128, 64, 32 };
    public override void OnInspectorGUI()
    {
        PlanerBBR bbr = (PlanerBBR)target;
        bbr.steps = 3;
        DrawDefaultInspector();
        EditorGUILayout.BeginHorizontal();        
        EditorGUILayout.LabelField("Height: from to", bbr.startHeight.ToString("0.00"));
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField(bbr.endHeight.ToString("0.00"), style);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.MinMaxSlider(ref bbr.startHeight, ref bbr.endHeight, 0, bbr.maxHeight);
        res_index = EditorGUILayout.Popup("Resolution",
             res_index, res_options.Select(x => x.ToString()).ToArray(), EditorStyles.popup);               
        if (res_options[res_index] != bbr.resolution)
        {
            bbr.resolution = res_options[res_index];
        }
        if (GUILayout.Button("Bake Textures"))
        {
            if (!bbr.cromakey_mat)
                bbr.cromakey_mat = AssetDatabase.LoadAssetAtPath("Assets/BlurBakeReflections/ChromaKey.mat", typeof(Material)) as Material;
            bbr.BuildProbe();
            for (int i = 0; i < bbr.steps; i++)
            {
                bbr.SetUpProbe(i);
                if (!BakeCustomReflectionProbe(bbr.probe_reflect, i, bbr.steps))
                    return;
                bbr.SetReflection(i);
            }
            bbr.Clean();
        }        
    }   

    private bool BakeCustomReflectionProbe(ReflectionProbe probe, int step, int steps)
    {       
        string targetExtension = probe.hdr ? "exr" : "png";
        string targetPath = "Assets/BlurBakeReflections/Probes";
        if (string.IsNullOrEmpty(targetPath))
            targetPath = "Assets";
        else if (Directory.Exists(targetPath) == false)
            Directory.CreateDirectory(targetPath);
        string fileName = probe.name + step.ToString() + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
        string path = Path.Combine(targetPath, fileName);       
        if (string.IsNullOrEmpty(path))
            return false;
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
        EditorUtility.DisplayProgressBar("Reflection Probe " + (step + 1).ToString() + "/" + steps.ToString(), "Baking " + path, (float)(step + 1) / steps);
        if (!Lightmapping.BakeReflectionProbe(probe, path))
        {
            Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
            return false;
        }
        EditorUtility.ClearProgressBar();
        return true;
    }
}
