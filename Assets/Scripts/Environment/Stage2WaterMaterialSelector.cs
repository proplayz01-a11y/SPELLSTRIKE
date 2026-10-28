using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class Stage2WaterMaterialSelector : MonoBehaviour
{
    [SerializeField] private Material desktopMaterial;
    [SerializeField] private Material mobileMaterial;

    public void Configure(Material desktop, Material mobile)
    {
        desktopMaterial = desktop;
        mobileMaterial = mobile;
        ApplyMaterial();
    }

    private void OnEnable() => ApplyMaterial();
    private void OnValidate() => ApplyMaterial();

    private void ApplyMaterial()
    {
        Renderer targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

#if UNITY_EDITOR
        Material selected = Application.isPlaying && Application.isMobilePlatform
            ? mobileMaterial
            : desktopMaterial;
#else
        Material selected = Application.isMobilePlatform ? mobileMaterial : desktopMaterial;
#endif

        if (selected != null && targetRenderer.sharedMaterial != selected)
            targetRenderer.sharedMaterial = selected;
    }
}
