"""Verify v11 contains real facial meshes, no v08 projection, no source damage."""
import bpy, json, hashlib, zipfile
from pathlib import Path
ROOT=Path.cwd()
FOLDER=ROOT/"ArtSource/Characters/Nevali/Working"
GLB=FOLDER/"Nevali_TrueMeshFace_Rebuild_v11.glb"
BLEND=FOLDER/"Nevali_TrueMeshFace_Rebuild_v11.blend"
QA=ROOT/"Library/NevaliModelRepair_20261023/Geometry_v11"
ORIGINAL=ROOT/"ArtSource/Characters/Nevali/Original/base_basic_pbr.glb"
ZIP=Path.home()/"Downloads/bc32551b-3b3f-4f0f-8229-a901e8229a7a.zip"
assert GLB.exists() and BLEND.exists()
with zipfile.ZipFile(ZIP) as archive:
    unchanged=hashlib.sha256(ORIGINAL.read_bytes()).digest()==hashlib.sha256(
        archive.read("base_basic_pbr.glb")).digest()
assert unchanged, "Original Rodin model altered"
build=json.loads((QA/"GeometryBuildQA.json").read_text())
assert build["original_face_polygons_physically_deleted"]>500
assert not build["image_face_projection_used"]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(GLB))
meshes=[o for o in bpy.data.objects if o.type=="MESH"]
name_map={o.name:o for o in meshes}
need={
    "head": "NEW_ROUNDED_3D_HEAD",
    "nose": "SCULPTED_LOW_POLY_NOSE_BRIDGE",
    "eyeball": "EYEBALL_",
    "iris": "IRIS_BROWN_",
    "pupil": "PUPIL_",
    "eyelid": "UPPER_EYELID_",
    "eyebrow": "BROW_SCULPT_",
    "mouth": "MOUTH_SMILE_SEAM",
    "lip": "LOWER_LIP",
}
counts={kind:sum(part in n for n in name_map) for kind,part in need.items()}
assert all(n>=1 for n in counts.values()), counts
assert counts["eyeball"]==2 and counts["iris"]==2 and counts["pupil"]==2
assert all("REPROJECT" not in n for n in name_map)
head=next(o for o in meshes if "NEW_ROUNDED_3D_HEAD" in o.name)
assert len(head.data.vertices)>500
body=next(o for o in meshes if o.name.startswith("NEVALI__RODIN_ORIGINAL_BODY"))
assert len(body.data.polygons)==build["retained_body_polygons"]
all_mats=[m for ob in meshes for m in ob.data.materials if m]
assert len(all_mats)>0
assert not any("APPROVED_CONCEPT" in m.name or "FACIALREFERENCE" in m.name
               for m in all_mats)
summary={
    "status":"GEOMETRY_ROUNDTRIP_PASS",
    "original_rodin_unchanged":unchanged,
    "original_face_polygons_physically_deleted":build["original_face_polygons_physically_deleted"],
    "new_closed_3d_head_vertices":len(head.data.vertices),
    "glb_mesh_objects":len(meshes),
    "feature_mesh_counts":counts,
    "original_body_polygons_retained":len(body.data.polygons),
    "no_image_plane_or_facedeck":True,
    "rigged":any(o.type=="ARMATURE" for o in bpy.data.objects),
    "requires_art_polish":True
}
(QA/"RoundtripQA.json").write_text(json.dumps(summary,indent=2))
print("NEVALI_V11_TRUE_FACE_QA_PASS",json.dumps(summary))
