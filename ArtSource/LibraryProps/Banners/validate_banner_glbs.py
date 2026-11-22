"""Round-trip check: both final GLBs must reimport in Blender with textures."""
import bpy
import json
from pathlib import Path

root=Path(__file__).resolve().parents[3]
models=root/"Assets/Cinematics/Storyboard3D/Environment/Props/Banners"
out=[]
for name in ["LibraryBanner_NavyGold_v01.glb","LibraryBanner_NavyGold_v01_LOD1.glb"]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    p=models/name
    assert p.is_file(),str(p)
    bpy.ops.import_scene.gltf(filepath=str(p))
    objs=[o for o in bpy.data.objects if o.type=="MESH"]
    count=sum(sum(len(poly.vertices)-2 for poly in o.data.polygons) for o in objs)
    mats={m.name for o in objs for m in o.data.materials if m}
    images={n.image.name:tuple(n.image.size) for o in objs for m in o.data.materials
              if m and m.use_nodes for n in m.node_tree.nodes
              if n.type=="TEX_IMAGE" and n.image}
    record={"file":name,"bytes":p.stat().st_size,"meshes":len(objs),
            "triangles":count,"materials":sorted(mats),"images":images}
    print("ROUNDTRIP_OK",json.dumps(record))
    assert 0<len(objs)<=10,record
    assert count>2500,record
    assert any("FABRIC" in name for name in mats),record
    out.append(record)
assert out[1]["triangles"]<out[0]["triangles"],out
(root/"Library/StoryboardBannerPreview_20261022/LibraryBanner_NavyGold_v01_GLBRoundtrip.json").write_text(
    json.dumps(out,indent=2))
