using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BBR
{

    [RequireComponent(typeof(MeshRenderer))]
    public class PlanerBBR : MonoBehaviour
    {
        // BBR requared assets
        [HideInInspector]
        public VolumeProfile Probe_PPP;
        [HideInInspector]
        public Material ChromaKey_Mat;

        public ComputeShader Mix_Shader;
        // Visible settings
        [Tooltip("Add your view point here. Default - Camera.main")]
        public Transform ViewPoint;        
        [Tooltip("Use the most rare color from scene.")]
        public Color ChromaKeyColor = Color.black;
        [Tooltip("More steps - more smooth result, more time to bake.")]
        [Range(2, 5)] public int Steps = 3;
        [Tooltip("Visualze system of cuts.")]
        public bool PreviewHeights = false;
        [Tooltip("Approximate height of scene. Use range to set up cuts.")]
        public float MaxSceneHeight = 2;
        // Customized
        [HideInInspector]
        public float StartHeight = 0.1f;
        [HideInInspector]
        public float EndHeight = 1.0f;
        [HideInInspector]
        public int Resolution = 2048;
        // Generated
        [HideInInspector]
        public Camera Probe_Camera;
        private Cubemap[] probe_steps_maps = new Cubemap[0];
        [HideInInspector]
        public Texture2D Probe_Map;
        private ReflectionProbe probe;
        private List<GameObject> heights = new List<GameObject>();

        private MeshRenderer plane_rend;
        private Material plane_source_mat;
        private ShadowCastingMode plane_shadow_mode;       
        private List<GameObject> childrens = new List<GameObject>(); // for children destroy
       
       

        public bool BuildProbe()
        {
            if (!this.transform.GetComponent<Renderer>().sharedMaterial.shader.name.Equals("Shader Graphs/PlanerBBR"))
            {
                Debug.LogError("Wrong Shader \"" + this.transform.GetComponent<Renderer>().sharedMaterial.shader.name + "\" use \"Shader Graphs/PlanerBBR\" instead");
                return false;
            }
            if (!ChromaKey_Mat)
            {
                Debug.LogError("Can't load material from \"Assets / BlurBakeReflections / ChromaKey.mat\"");
                return false;
            }
            if (!Mix_Shader)
            {
                Debug.LogError("Can't load shader from \"Assets / BlurBakeReflections / MixCubemapsBBR.compute\"");
                return false;
            }
            if (!ChromaKey_Mat.shader.name.Equals("Shader Graphs/PlanerChromaKey"))
            {
                Debug.LogError("Wrong Shader \"" + ChromaKey_Mat.shader.name + "\" use \"Shader Graphs/PlanerChromaKey\" instead");
                return false;
            }            
            if (!ViewPoint)
            {
                Debug.LogError("No camera attached");
                return false;
            }
            if (!Probe_PPP)
            {
                Debug.LogError("Can't load volume profile from \"Assets/BlurBakeReflections/BBR_Probes_PPP.asset\"");
                return false;
            }
            /*
             * Prepare Renderer
             */
            ClearHeights();
            if (!plane_rend)
            {
                plane_rend = transform.GetComponent<MeshRenderer>();
                plane_rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
            plane_shadow_mode = plane_rend.shadowCastingMode;
            plane_rend.shadowCastingMode = ShadowCastingMode.Off;
            plane_rend.sharedMaterial.SetColor("_ChromaKey", ChromaKeyColor);
            plane_source_mat = plane_rend.sharedMaterial;
            plane_rend.sharedMaterial = ChromaKey_Mat;
            ChromaKey_Mat.SetColor("_ChromaKey", ChromaKeyColor);
            ChromaKey_Mat.SetFloat("_Alpha", 1.0f);
            /*
             * Prepare ReflectionProbe
             */
            if (probe_steps_maps.Length != 0)
                Array.Clear(probe_steps_maps, 0, 3);
            probe_steps_maps = new Cubemap[] { new Cubemap(Resolution, TextureFormat.RGBAHalf, false),
                new Cubemap((int)((float)Resolution / 2), TextureFormat.RGBAHalf, false),
                new Cubemap((int)((float)Resolution / 4), TextureFormat.RGBAHalf, false) };
            /*foreach (Cubemap tex in probe_steps_maps)
            {
                tex.hideFlags = HideFlags.HideAndDontSave;
                tex.dimension = TextureDimension.Cube;
            }*/
            GameObject probe = (GameObject)Instantiate(ViewPoint.gameObject, this.transform);
            probe.transform.position = ViewPoint.position - this.transform.up * (Vector3.Project(ViewPoint.position - this.transform.position, this.transform.up).magnitude) * 2;
            /*probe.transform.rotation = Quaternion.identity; //Quaternion.Inverse(Quaternion.identity);
            probe.transform.rotation = Quaternion.FromToRotation(probe.transform.up, -probe.transform.up);*/
            probe.hideFlags = HideFlags.HideAndDontSave;
            Volume ppp = probe.AddComponent<Volume>();
            ppp.profile = Probe_PPP;
            Probe_Camera = probe.transform.GetComponent<Camera>();
            Probe_Camera.enabled = false;
            // ready to go
            return true;
        }
        public void RenderProbeStep(int step)
        {
            if (step + 1 == Steps)
            {
                plane_rend.sharedMaterial.SetFloat("_Height", 0);
                plane_rend.sharedMaterial = plane_source_mat;
            }
            else
                plane_rend.sharedMaterial.SetFloat("_Height", StartHeight + (EndHeight - StartHeight) * (float)step);            
            Probe_Camera.RenderToCubemap(probe_steps_maps[step]);
            plane_source_mat.SetTexture("_Reflection_" + step, probe_steps_maps[step]);
        }
        public void CombineReflections()
        {
            //Probe_Map = probe_steps_maps[0];

            Probe_Map = new Texture2D(Resolution * 6, Resolution, TextureFormat.RGBAHalf, true);
            int kernelHandle = Mix_Shader.FindKernel("CSMain");
            Mix_Shader.SetTexture(kernelHandle, "Result", Probe_Map);
            Mix_Shader.SetTexture(kernelHandle, "FirstMap", Cubemap2Texture2D(probe_steps_maps[0]));
            Mix_Shader.Dispatch(kernelHandle, Resolution / 8, Resolution / 8, 1);

            //plane_source_mat.SetTexture("_Reflection_0", probe_steps_maps[0]);

        }
        public void Clean() // after bake
        {
            DestroyImmediate(Probe_Camera.gameObject);
            Probe_Camera = null;
            plane_rend.shadowCastingMode = plane_shadow_mode;
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
                ChromaKey_Mat.SetColor("_ChromaKey", ChromaKeyColor);
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
        private void ClearHeights()
        {
            this.PreviewHeights = false;
            UpdateHeights();
        }
        private Texture2D Cubemap2Texture2D(Cubemap cubemap)
        {
            var tex = new Texture2D(cubemap.width * 6, cubemap.height, TextureFormat.RGBAHalf, true);
            // Combine cubemap sides to one Texture2D
            tex.SetPixels(0, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.PositiveX));
            tex.SetPixels(cubemap.width, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.NegativeX));
            tex.SetPixels(cubemap.width * 2, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.PositiveY));
            tex.SetPixels(cubemap.width * 3, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.NegativeY));
            tex.SetPixels(cubemap.width * 4, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.PositiveZ));
            tex.SetPixels(cubemap.width * 5, 0, cubemap.width, cubemap.width, cubemap.GetPixels(CubemapFace.NegativeZ));
            return tex;
        }
        private void Awake() // destroy all helpers when game start
        {
            ClearHeights();
        }
    }
}
