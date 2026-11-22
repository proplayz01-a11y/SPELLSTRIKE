"""Round-trip the actual exported GLBs and check Unity flame marker nodes."""
import bpy
import json
from pathlib import Path

project=Path(__file__).resolve().parents[3]
exports=project/"Assets/Cinematics/Storyboard3D/Environment/Props/Chandeliers"
check=[]
for fname in ("StudyChandelier_BrassRing_v01.glb",
              "StudyChandelier_BrassRing_v01_LOD1.glb",
              "StudyChandelier_BrassRing_v01_LOD2.glb"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    filepath=exports/fname
    assert filepath.exists(),filepath
    bpy.ops.import_scene.gltf(filepath=str(filepath))
    objs=[o for o in bpy.data.objects if o.type=="MESH"]
    anchors=sorted(o.name for o in bpy.data.objects
                   if o.name.startswith("FlameSocket_"))
    tris=sum(sum(len(poly.vertices)-2 for poly in o.data.polygons) for o in objs)
    matnames=sorted({m.name for o in objs for m in o.data.materials if m})
    report={"file":fname,"bytes":filepath.stat().st_size,"triangles":tris,
            "meshes":len(objs),"material_names":matnames,
            "flame_anchors":anchors}
    assert len(objs)<=8,report
    assert len(anchors)==8,report
    assert 0<tris<30000,report
    assert any("WAX" in n for n in matnames),report
    assert any("METAL" in n for n in matnames),report
    print("ROUNDTRIP_PASS",json.dumps(report))
    check.append(report)
assert check[1]["triangles"]<check[0]["triangles"],check
assert check[2]["triangles"]<check[1]["triangles"],check
(project/"Library/StoryboardChandelierPreview_20261022/StudyChandelier_BrassRing_v01_GLBRoundtrip.json").write_text(
   json.dumps(check,indent=2))
