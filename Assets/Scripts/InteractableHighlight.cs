//  Outline.cs
//  QuickOutline (Modified for Interactable Integration)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class InteractableHighlight : MonoBehaviour {
    private static HashSet<Mesh> registeredMeshes = new HashSet<Mesh>();

    public enum Mode {
        OutlineAll,
        OutlineVisible,
        OutlineHidden,
        OutlineAndSilhouette,
        SilhouetteOnly
    }

    public enum HighlightState {
        None,       // 光っていない
        Proximity,  // 近づいた（接近）
        Gaze        // 視線が合った（注視）
    }

    [Header("Highlight Settings")]
    public bool isHighlightable = true; 
    
    public bool isSuppressed = false; 

    public Color proximityColor = new Color(1f, 1f, 1f, 0.5f); 
    public Color gazeColor = Color.yellow; 

    private HighlightState currentState = HighlightState.None;

    public Mode OutlineMode {
        get { return outlineMode; }
        set {
            outlineMode = value;
            needsUpdate = true;
        }
    }

    public Color OutlineColor {
        get { return outlineColor; }
        set {
            outlineColor = value;
            needsUpdate = true;
        }
    }

    public float OutlineWidth {
        get { return outlineWidth; }
        set {
            outlineWidth = value;
            needsUpdate = true;
        }
    }

    [Serializable]
    private class ListVector3 {
        public List<Vector3> data;
    }

    [SerializeField]
    private Mode outlineMode;

    [SerializeField, HideInInspector]
    private Color outlineColor = Color.white;

    [SerializeField, Range(0f, 10f)]
    private float outlineWidth = 2f;

    [Header("Optional")]
    [SerializeField]
    private bool precomputeOutline;

    [SerializeField, HideInInspector]
    private List<Mesh> bakeKeys = new List<Mesh>();

    [SerializeField, HideInInspector]
    private List<ListVector3> bakeValues = new List<ListVector3>();

    private Renderer[] renderers;
    private Material outlineMaskMaterial;
    private Material outlineFillMaterial;

    private bool needsUpdate;

    void Awake() {
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        renderers = GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.layer != ignoreLayer)
            .ToArray();

        outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
        outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

        outlineMaskMaterial.name = "OutlineMask (Instance)";
        outlineFillMaterial.name = "OutlineFill (Instance)";

        LoadSmoothNormals();
        needsUpdate = true;
    }

    void Start() {
        this.enabled = false;
    }

    public void ChangeHighlightState(HighlightState newState) {
        if (!isHighlightable || isSuppressed) {
            if (this.enabled) this.enabled = false;
            currentState = HighlightState.None;
            return;
        }

        if (currentState == newState) return;

        currentState = newState;

        switch (currentState) {
            case HighlightState.None:
                this.enabled = false; 
                break;
            case HighlightState.Proximity:
                OutlineColor = proximityColor; 
                this.enabled = true;           
                break;
            case HighlightState.Gaze:
                OutlineColor = gazeColor;      
                this.enabled = true;           
                break;
        }
    }

    void OnEnable() {
        foreach (var renderer in renderers) {
            var materials = renderer.sharedMaterials.ToList();
            materials.Add(outlineMaskMaterial);
            materials.Add(outlineFillMaterial);
            renderer.sharedMaterials = materials.ToArray();
        }
    }

    void OnValidate() {
        needsUpdate = true;
        if (!precomputeOutline && bakeKeys.Count != 0 || bakeKeys.Count != bakeValues.Count) {
            bakeKeys.Clear();
            bakeValues.Clear();
        }
        if (precomputeOutline && bakeKeys.Count == 0) {
            Bake();
        }
    }

    void Update() {
        if (needsUpdate) {
            needsUpdate = false;
            UpdateMaterialProperties();
        }
    }

    void OnDisable() {
        foreach (var renderer in renderers) {
            var materials = renderer.sharedMaterials.ToList();
            materials.Remove(outlineMaskMaterial);
            materials.Remove(outlineFillMaterial);
            renderer.sharedMaterials = materials.ToArray();
        }
    }

    void OnDestroy() {
        Destroy(outlineMaskMaterial);
        Destroy(outlineFillMaterial);
    }

    void Bake() {
        var bakedMeshes = new HashSet<Mesh>();
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
            if (meshFilter.gameObject.layer == ignoreLayer) continue; // 💡ここも液体を除外
            if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;
            var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
            bakeKeys.Add(meshFilter.sharedMesh);
            bakeValues.Add(new ListVector3() { data = smoothNormals });
        }
    }

    void LoadSmoothNormals() {
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
            if (meshFilter.gameObject.layer == ignoreLayer) continue; // 💡ここも液体を除外
            if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;
            var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
            var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);
            meshFilter.sharedMesh.SetUVs(3, smoothNormals);
            var renderer = meshFilter.GetComponent<Renderer>();
            if (renderer != null) CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
        }
        foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
            if (skinnedMeshRenderer.gameObject.layer == ignoreLayer) continue; // 💡ここも液体を除外
            if (!registeredMeshes.Add(skinnedMeshRenderer.sharedMesh)) continue;
            skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
            CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
        }
    }

    List<Vector3> SmoothNormals(Mesh mesh) {
        var groups = mesh.vertices.Select((vertex, index) => new KeyValuePair<Vector3, int>(vertex, index)).GroupBy(pair => pair.Key);
        var smoothNormals = new List<Vector3>(mesh.normals);
        foreach (var group in groups) {
            if (group.Count() == 1) continue;
            var smoothNormal = Vector3.zero;
            foreach (var pair in group) smoothNormal += smoothNormals[pair.Value];
            smoothNormal.Normalize();
            foreach (var pair in group) smoothNormals[pair.Value] = smoothNormal;
        }
        return smoothNormals;
    }

    void CombineSubmeshes(Mesh mesh, Material[] materials) {
        if (mesh.subMeshCount == 1) return;
        if (mesh.subMeshCount > materials.Length) return;
        mesh.subMeshCount++;
        mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
    }

    void UpdateMaterialProperties() {
        outlineFillMaterial.SetColor("_OutlineColor", outlineColor);
        switch (outlineMode) {
            case Mode.OutlineAll:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;
            case Mode.OutlineVisible:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;
            case Mode.OutlineHidden:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;
            case Mode.OutlineAndSilhouette:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always);
                outlineFillMaterial.SetFloat("_OutlineWidth", outlineWidth);
                break;
            case Mode.SilhouetteOnly:
                outlineMaskMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                outlineFillMaterial.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Greater);
                outlineFillMaterial.SetFloat("_OutlineWidth", 0f);
                break;
        }
    }

    private bool isGazed = false;
    private bool isProximate = false;

    public void SetGaze(bool isLooking) {
        isGazed = isLooking;
        EvaluateHighlight();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            isProximate = true;
            EvaluateHighlight();
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            isProximate = false;
            EvaluateHighlight();
        }
    }

    private void EvaluateHighlight() {
        if (!isHighlightable || isSuppressed) {
            ChangeHighlightState(HighlightState.None);
            return;
        }

        if (DialogueManager.Instance != null && DialogueManager.Instance.isTalking) {
            ChangeHighlightState(HighlightState.None);
            return;
        }

        if (isGazed) {
            ChangeHighlightState(HighlightState.Gaze);
        } else if (isProximate) {
            ChangeHighlightState(HighlightState.Proximity);
        } else {
            ChangeHighlightState(HighlightState.None);
        }
    }

    private void OnMouseEnter() {

      // if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
      //     return; 
      // }
        if (Cursor.visible) {
            SetGaze(true);
        }
    }

    private void OnMouseExit() {
        SetGaze(false);
    }
}