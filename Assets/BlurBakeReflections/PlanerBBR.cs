using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
public class PlanerBBR : MonoBehaviour
{
    [Tooltip("Add your camera here. Default - Camera.main")]
    public Transform ViewPoint;
    [Tooltip("Use the most rare color from scene.")]
    public Color ChromaKey = Color.black;
    [HideInInspector]
    public Material ChromaKey_Mat;
    [HideInInspector]
    public int Steps = 3;
    [Tooltip("Visualze system of cuts.")]
    public bool PreviewHeights = false;
    private List<GameObject> heights = new List<GameObject>();
    private List<GameObject> childrens = new List<GameObject>();
    [Tooltip("Approximate height of scene. Use range to set up cuts.")]
    public float MaxSceneHeight = 2;
    [HideInInspector]
    public float StartHeight = 0.1f;
    [HideInInspector]
    public float EndHeight = 1.0f;
    [HideInInspector]
    public int Resolution = 2048;
    [HideInInspector]
    public ReflectionProbe Probe_Reflect;

    private MeshRenderer rend;
    private Material source_mat;
    private ShadowCastingMode shadow_mode;

    public bool BuildProbe()
    {
        if (this.transform.GetComponent<Renderer>().sharedMaterial.shader.name != "Shader Graphs/PlanerBBR")
        {
            Debug.LogError("Wrong Shader \"" + this.transform.GetComponent<Renderer>().sharedMaterial.shader.name + "\" use \"Shader Graphs/PlanerBBR\" instead");
            return false;
        }
        if (!ChromaKey_Mat)
        {
            Debug.LogError("Can't load material from \"Assets / BlurBakeReflections / ChromaKey.mat\"");
            return false;
        }
        if (ChromaKey_Mat.shader.name != "Shader Graphs/PlanerChromaKey")
        {
            Debug.LogError("Wrong Shader \"" + ChromaKey_Mat.shader.name + "\" use \"Shader Graphs/PlanerChromaKey\" instead");
            return false;
        }
        if (!ViewPoint)
        {
            Debug.LogError("No camera attached");
            return false;
        }

        ClearHeights();
        /*
         * Prepare Renderer
         */
        if (!rend)
        {
            rend = transform.GetComponent<MeshRenderer>();
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }       
        shadow_mode = rend.shadowCastingMode;
        rend.shadowCastingMode = ShadowCastingMode.Off;        
        rend.sharedMaterial.SetColor("_ChromaKey", ChromaKey);
        source_mat = rend.sharedMaterial;
        rend.sharedMaterial = ChromaKey_Mat;
        ChromaKey_Mat.SetColor("_ChromaKey", ChromaKey);
        ChromaKey_Mat.SetFloat("_Alpha", 1.0f);
        /*
         * Prepare ReflectionProbe
         */
        GameObject probe = new GameObject("BBRProbe", typeof(ReflectionProbe));
        probe.transform.SetParent(this.transform);
        Probe_Reflect = probe.GetComponent<ReflectionProbe>();
        Probe_Reflect.mode = ReflectionProbeMode.Custom;
        Probe_Reflect.resolution = Resolution;        
        probe.transform.position = ViewPoint.position - this.transform.up * (Vector3.Project(ViewPoint.position - transform.position, this.transform.up).magnitude) * 2;
        return true;
    }

    public void UpdateHeights()
    {
        Transform parent = this.transform.Find("BBRHeights");
        if (!parent && heights.Count != 0) // Clear List without parent
            heights.Clear();
        if (parent && heights.Count == 0) // Destroy parent whith empty List
            DestroyAllChildren(parent, this.transform);
        if (parent && heights.Count != 0) // Check if parent has children and they on the List
            foreach (GameObject height in heights)           
                if (!height)
                {
                    heights.Clear();
                    DestroyAllChildren(parent, this.transform);
                    break;
                }
        if (PreviewHeights && heights.Count == 0) // Set preview on
        {
            GameObject new_parent = new GameObject("BBRHeights"); // Create empty parent
            new_parent.transform.SetParent(this.transform);
            ZeroTransform(new_parent.transform);
            for (int i = 0; i < 2; i++) // Create children = heights
            {
                GameObject height = (GameObject)Instantiate(this.gameObject, new_parent.transform);
                DestroyImmediate(height.GetComponent<PlanerBBR>());
                DestroyAllChildren(height.transform);
                ZeroTransform(height.transform);
                height.name = "height " + (i);
                height.tag = "EditorOnly";
                height.isStatic = false;
                MeshRenderer h_rend = height.GetComponent<MeshRenderer>();
                h_rend.sharedMaterial = ChromaKey_Mat;
                h_rend.shadowCastingMode = ShadowCastingMode.Off;
                h_rend.receiveShadows = false;
                heights.Add(height); // Put them on the List
            }
        }
        if (!PreviewHeights && heights.Count != 0)  // Set preview off
        {
            heights.Clear();
            DestroyAllChildren(parent, this.transform);
        }
    }

    private void ZeroTransform(Transform trans)
    {
        trans.transform.localPosition = Vector3.zero;
        trans.transform.localEulerAngles = Vector3.zero;
        trans.transform.localScale = Vector3.one;
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
            child = parent.Find(c_name);
        }
    }

    public void SetUpPreviewHeights()
    {
        if (heights.Count == 2)
        {
            if (heights[0])
                heights[0].transform.position = this.transform.position + StartHeight * this.transform.up;
            if (heights[1])
                heights[1].transform.position = this.transform.position + EndHeight * this.transform.up;
            ChromaKey_Mat.SetFloat("_Height", 0.0f);
            ChromaKey_Mat.SetFloat("_Alpha", 0.25f);
            ChromaKey_Mat.SetColor("_ChromaKey", ChromaKey);
        }
    }

    public void SetUpProbe(int step)
    {
        if (step + 1 == Steps)
        {
            rend.sharedMaterial.SetFloat("_Height", 0);
            rend.sharedMaterial = source_mat;
        }
        else        
            rend.sharedMaterial.SetFloat("_Height", StartHeight + (EndHeight - StartHeight) * (float)step);         
    }

    public void SetReflection(int step)
    {
        source_mat.SetTexture("_Reflection_" + step, Probe_Reflect.customBakedTexture);
    }

    public void Clean() // after bake
    {
        DestroyImmediate(Probe_Reflect.gameObject);
        Probe_Reflect = null;
        rend.shadowCastingMode = shadow_mode;
    }
    public void ClearHeights()
    {
        this.PreviewHeights = false;
        UpdateHeights();
    }
    private void Awake() // destroy all helpers when game start
    {
        ClearHeights();
    }
}
