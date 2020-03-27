﻿using UnityEngine;
using UnityEngine.Rendering;

public class PlanerBBR : MonoBehaviour
{
    public Color chromakey;
    [HideInInspector]
    public Material cromakey_mat;
    private Material source_mat;
    public AnimationCurve spread;
    public float maxHeight = 10;
    [HideInInspector]
    public float startHeight;
    [HideInInspector]
    public float endHeight;
    [HideInInspector]
    public int start_resolution;
    [HideInInspector]
    public int steps = 5;
    [HideInInspector]
    public ReflectionProbe probe_reflect;
    private MeshRenderer rend;
    private ShadowCastingMode shadow_mode;

    public void BuildProbe()
    {
        if (!rend)
        {
            rend = transform.GetComponent<MeshRenderer>();
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }       
        shadow_mode = rend.shadowCastingMode;
        rend.shadowCastingMode = ShadowCastingMode.Off;        
        rend.sharedMaterial.SetColor("_ChromaKey", chromakey);
        source_mat = rend.sharedMaterial;
        rend.sharedMaterial = cromakey_mat;
        rend.sharedMaterial.SetColor("_ChromaKey", chromakey);
        GameObject probe = new GameObject("BBRProbe", typeof(ReflectionProbe));
        probe.transform.SetParent(this.transform);
        probe_reflect = probe.GetComponent<ReflectionProbe>();
        probe_reflect.mode = ReflectionProbeMode.Custom;
        Transform cam = Camera.main.transform;
        probe.transform.position = cam.position - Vector3.up * (cam.position.y - transform.position.y) * 2;          
    }

    public void SetUpProbe(int step)
    {
        if (step + 1 == steps)
        {
            rend.sharedMaterial.SetFloat("_Height", 0);
            rend.sharedMaterial = source_mat;
        }
        else
        {
            rend.sharedMaterial.SetFloat("_Height", startHeight + (endHeight - startHeight) * spread.Evaluate((float)(step + 1) / steps));            
        }
        probe_reflect.resolution = (int)((float)start_resolution / Mathf.Pow(2, step));
    }

    public void SetReflection(int step)
    {
        source_mat.SetTexture("_Reflection_" + step, probe_reflect.customBakedTexture);
    }

    public void Clean()
    {
        DestroyImmediate(probe_reflect.gameObject);
        probe_reflect = null;
        rend.shadowCastingMode = shadow_mode;
    }
}
