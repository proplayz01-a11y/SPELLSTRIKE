"""GLB import, geometry, texture and material QA for both standalone props."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[2]
OUT=PROJECT/"Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner"
QA=PROJECT/"Library/StoryboardScholarCornerPreview_20261022"
variants=[
 ("ScholarSideTable_Walnut_v01.glb",True,False),
 ("ScholarSideTable_Walnut_v01_LOD1.glb",True,True),
 ("AntiqueCelestialGlobe_Brass_v01.glb",False,False),
 ("AntiqueCelestialGlobe_Brass_v01_LOD1.glb",False,True)
]
results={}
for name,is_table,is_lod1 in variants:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    file=OUT/name
    assert file.is_file(),file
    bpy.ops.import_scene.gltf(filepath=str(file))
    allmeshes=[o for o in bpy.data.objects if o.type=="MESH"]
    triangles=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in allmeshes)
    names=[o.name for o in bpy.data.objects]
    mats={m.name for ob in allmeshes for m in ob.data.materials if m}
    imgs={}
    for m in bpy.data.materials:
        if not m.use_nodes:continue
        for n in m.node_tree.nodes:
            if n.type=="TEX_IMAGE" and n.image:
                imgs[n.image.name]=list(n.image.size)
    bbox=[o.matrix_world@Vector(p) for o in allmeshes for p in o.bound_box]
    ext=[max(p[i] for p in bbox)-min(p[i] for p in bbox) for i in range(3)]
    record={
      'filename':name,'mesh_count':len(allmeshes),'triangles':triangles,
      'materials':sorted(mats),'textures':imgs,
      'bounding_size_m':[round(v,3) for v in ext],
      'file_bytes':file.stat().st_size}
    assert 1<=len(allmeshes)<=6,record
    assert triangles>500,record
    if is_table:
        assert "WALNUT_AgedWood" in mats,record
        assert 1.47<ext[0]<1.52 and .73<ext[1]<.80,record
    else:
        assert "GLOBE_WornParchmentCartography" in mats,record
        assert len(imgs)>=1,record
        assert .55<max(ext[:2])<.90 and .60<ext[2]<1.0,record
    if not is_lod1:
        assert len(imgs)>=1,record
    results[name]=record
    print("GLB_ROUNDTRIP_PASS",json.dumps(record))
assert results[variants[1][0]]['triangles']<results[variants[0][0]]['triangles']
assert results[variants[3][0]]['triangles']<results[variants[2][0]]['triangles']
(QA/'ScholarCorner_v01_GLBRoundtrip.json').write_text(json.dumps(results,indent=2))
print("ALL_FOUR_GLBS_VALIDATED")
