using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlanerBBR : MonoBehaviour
{
    public Color chromakey = Color.black;
    [HideInInspector]
    public Material chromakey_mat;
    [HideInInspector]
    public int steps = 3;
    public bool previewHeights = false;
    private List<GameObject> Heights = new List<GameObject>();
    private List<GameObject> childrens = new List<GameObject>();
    public float maxSceneHeight = 10;
    [HideInInspector]
    public float startHeight = 0.1f;
    [HideInInspector]
    public float endHeight = 1.0f;
    [HideInInspector]
    public int resolution = 2048;
    [HideInInspector]
    public ReflectionProbe probe_reflect;
    private MeshRenderer rend;
    private Material source_mat;
    private ShadowCastingMode shadow_mode;

    public bool BuildProbe()
    {
        ClearHeights();
        if (this.transform.GetComponent<Renderer>().sharedMaterial.shader.name != "Shader Graphs/PlanerBBR")
        {
            Debug.LogError("Wrong Shader \"" + this.transform.GetComponent<Renderer>().sharedMaterial.shader.name + "\" use \"Shader Graphs/PlanerBBR\" instead");
            return false;
        }
        if (!chromakey_mat)
        {
            Debug.LogError("Can't load material at \"Assets / BlurBakeReflections / ChromaKey.mat\"");
            return false;
        }
        if (chromakey_mat.shader.name != "Shader Graphs/PlanerChromaKey")
        {
            Debug.LogError("Wrong Shader \"" + chromakey_mat.shader.name + "\" use \"Shader Graphs/PlanerChromaKey\" instead");
            return false;
        }
        if (!rend)
        {
            rend = transform.GetComponent<MeshRenderer>();
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }       
        shadow_mode = rend.shadowCastingMode;
        rend.shadowCastingMode = ShadowCastingMode.Off;        
        rend.sharedMaterial.SetColor("_ChromaKey", chromakey);
        source_mat = rend.sharedMaterial;
        rend.sharedMaterial = chromakey_mat;
        chromakey_mat.SetColor("_ChromaKey", chromakey);
        chromakey_mat.SetFloat("_Alpha", 1.0f);
        GameObject probe = new GameObject("BBRProbe", typeof(ReflectionProbe));
        probe.transform.SetParent(this.transform);
        probe_reflect = probe.GetComponent<ReflectionProbe>();
        probe_reflect.mode = ReflectionProbeMode.Custom;
        probe_reflect.resolution = resolution;
        Transform cam = Camera.main.transform;
        probe.transform.position = cam.position - this.transform.up * (Vector3.Project(cam.position - transform.position, this.transform.up).magnitude) * 2;
        return true;
    }

    public void UpdateHeights()
    {
        Transform parent = this.transform.Find("BBRHeights");
        if (!parent && Heights.Count != 0)
        {
            Heights.Clear();
        }
        if (parent && Heights.Count == 0)
        {
            DestroyAllChildren(parent);
        }
        if (parent && Heights.Count != 0)
        {
            foreach (GameObject height in Heights)
            {
                if (!height)
                {
                    Heights.Clear();
                    DestroyAllChildren(parent);
                    break;
                }
            }
        }
        if (previewHeights && Heights.Count == 0)
        {
            GameObject new_parent = new GameObject("BBRHeights");
            new_parent.transform.SetParent(this.transform);
            new_parent.transform.localPosition = Vector3.zero;
            new_parent.transform.localEulerAngles = Vector3.zero;
            new_parent.transform.localScale = Vector3.one;
            for (int i = 0; i < 2; i++)
            {
                GameObject height = (GameObject)Instantiate(this.gameObject, new_parent.transform);
                DestroyImmediate(height.GetComponent<PlanerBBR>());
                DestroyAllChildren(height.transform);
                height.transform.localPosition = Vector3.zero;
                height.transform.localEulerAngles = Vector3.zero;
                height.transform.localScale = Vector3.one;
                height.name = "height " + (i + 1);
                height.tag = "EditorOnly";
                height.isStatic = false;
                MeshRenderer h_rend = height.GetComponent<MeshRenderer>();
                h_rend.sharedMaterial = chromakey_mat;
                h_rend.shadowCastingMode = ShadowCastingMode.Off;
                h_rend.receiveShadows = false;
                Heights.Add(height);
            }
            chromakey_mat.SetFloat("_Alpha", 0.25f);
        }
        if (!previewHeights && Heights.Count != 0)
        {
            Heights.Clear();
            DestroyAllChildren(parent);
        }
    }

    private void DestroyAllChildren(Transform parent)
    {
        childrens.Clear();
        for (int c = 0; c < parent.transform.childCount; c++)
            childrens.Add(parent.transform.GetChild(c).gameObject);
        foreach (GameObject child in childrens)
            DestroyImmediate(child);
        childrens.Clear();
    }
    private void DestroyAllChildren(string name)
    {
       
    }
    private void DestroyAllChildren(Transform child, Transform parent)
    {
        string c_name = child.name;
        while (child)
        {
            DestroyImmediate(child.gameObject);
            child = this.transform.Find(c_name);
        }
    }
    public void SetUpPreviewHeights()
    {
        if (Heights.Count == 2)
        {
            if (Heights[0])
                Heights[0].transform.position = this.transform.position + startHeight * this.transform.up;
            if (Heights[1])
                Heights[1].transform.position = this.transform.position + endHeight * this.transform.up;
            chromakey_mat.SetFloat("_Alpha", 0.5f);
            chromakey_mat.SetColor("_ChromaKey", chromakey);
        }
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
            rend.sharedMaterial.SetFloat("_Height", startHeight + (endHeight - startHeight) * (float)step);
        }        
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
    public void ClearHeights()
    {
        this.previewHeights = false;
        UpdateHeights();
    }
    private void Awake()
    {
        ClearHeights();
    }
}
