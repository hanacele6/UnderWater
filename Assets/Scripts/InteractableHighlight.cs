//  Outline.cs
//  QuickOutline (Modified for Interactable Integration)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

  // ==========================================
  // ★追加：ハイライトの状態を定義
  // ==========================================
  public enum HighlightState {
    None,       // 光っていない
    Proximity,  // 近づいた（接近）
    Gaze        // 視線が合った（注視）
  }

  // ==========================================
  // インタラクト用の統合変数
  // ==========================================
  [Header("Highlight Settings")]
  [Tooltip("このオブジェクトが現在光ることを許可されているか")]
  public bool isHighlightable = true; 

  [Tooltip("プレイヤーが近づいた時のフチ取り色")]
  public Color proximityColor = new Color(1f, 1f, 1f, 0.5f); // デフォルトは半透明の白
  
  [Tooltip("プレイヤーの視線が合った時のフチ取り色")]
  public Color gazeColor = Color.yellow; // デフォルトは黄色

  // 現在の状態を保持
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

  [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
  + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
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
    renderers = GetComponentsInChildren<Renderer>();

    outlineMaskMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineMask"));
    outlineFillMaterial = Instantiate(Resources.Load<Material>(@"Materials/OutlineFill"));

    outlineMaskMaterial.name = "OutlineMask (Instance)";
    outlineFillMaterial.name = "OutlineFill (Instance)";

    LoadSmoothNormals();
    needsUpdate = true;
  }

  // ==========================================
  // ゲーム開始時に自動でOFFにする
  // ==========================================
  void Start() {
      this.enabled = false;
  }

  // ==========================================
  // ★変更：PlayerInteract等から呼ばれる状態変更メソッド
  // ==========================================
  public void ChangeHighlightState(HighlightState newState) {
      if (!isHighlightable) {
          this.enabled = false;
          currentState = HighlightState.None;
          return;
      }

      // 状態が変わっていないなら何もしない（無駄な処理を省く）
      if (currentState == newState) return;

      currentState = newState;

      switch (currentState) {
          case HighlightState.None:
              this.enabled = false; // コンポーネントをOFFにしてフチ取りを消す
              break;
          case HighlightState.Proximity:
              OutlineColor = proximityColor; // 接近用の色に変更
              this.enabled = true;           // コンポーネントをONにしてフチ取りを出す
              break;
          case HighlightState.Gaze:
              OutlineColor = gazeColor;      // 視線用の色に変更
              this.enabled = true;           // コンポーネントをONにしてフチ取りを出す
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
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
      if (!bakedMeshes.Add(meshFilter.sharedMesh)) continue;
      var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
      bakeKeys.Add(meshFilter.sharedMesh);
      bakeValues.Add(new ListVector3() { data = smoothNormals });
    }
  }

  void LoadSmoothNormals() {
    foreach (var meshFilter in GetComponentsInChildren<MeshFilter>()) {
      if (!registeredMeshes.Add(meshFilter.sharedMesh)) continue;
      var index = bakeKeys.IndexOf(meshFilter.sharedMesh);
      var smoothNormals = (index >= 0) ? bakeValues[index].data : SmoothNormals(meshFilter.sharedMesh);
      meshFilter.sharedMesh.SetUVs(3, smoothNormals);
      var renderer = meshFilter.GetComponent<Renderer>();
      if (renderer != null) CombineSubmeshes(meshFilter.sharedMesh, renderer.sharedMaterials);
    }
    foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>()) {
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

  // ==========================================
  // ★追加：状態を管理するフラグ
  // ==========================================
  private bool isGazed = false;
  private bool isProximate = false;

  // PlayerInteractから呼ばれる（視線のON/OFF）
  public void SetGaze(bool isLooking) {
      isGazed = isLooking;
      EvaluateHighlight();
  }

  // プレイヤーが近づいた時（接近のON）
  private void OnTriggerEnter(Collider other) {
      // プレイヤーが近づいたかタグで判定（Player側のタグを"Player"に設定しておくこと）
      if (other.CompareTag("Player")) {
          isProximate = true;
          EvaluateHighlight();
      }
  }

  // プレイヤーが離れた時（接近のOFF）
  private void OnTriggerExit(Collider other) {
      if (other.CompareTag("Player")) {
          isProximate = false;
          EvaluateHighlight();
      }
  }

  // フラグの優先順位を計算して色を決定する
  private void EvaluateHighlight() {
      if (!isHighlightable) {
          ChangeHighlightState(HighlightState.None);
          return;
      }

      // 優先順位: 視線(Gaze) > 接近(Proximity) > 何もなし(None)
      if (isGazed) {
          ChangeHighlightState(HighlightState.Gaze);
      } else if (isProximate) {
          ChangeHighlightState(HighlightState.Proximity);
      } else {
          ChangeHighlightState(HighlightState.None);
      }
  }

}