"""Blender reimport / topology/anchor checks for all three standalone GLBs."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

HERE=Path(__file__).resolve().parent
PROJ=HERE.parents[2]
EX=PROJ/"Assets/Cinematics/Storyboard3D/Environment/Architecture/GothicGateway"
QA=PROJ/"Library/StoryboardGatewayPreview_20261022"
variants=[
   "StudyArchive_GothicGateway_v01.glb",
   "StudyArchive_GothicGateway_v01_LOD1.glb",
   "StudyArchive_GothicGateway_v01_LOD2.glb",
]
results=[]
for file in variants:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path=EX/file
    assert path.is_file(),path
    bpy.ops.import_scene.gltf(filepath=str(path))
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    materials=sorted({m.name for ob in meshes for m in ob.data.materials if m})
    hints=sorted(o.name for o in bpy.data.objects
                 if o.name.endswith("_PLACEMENT_HINT"))
    triangles=sum(sum(len(p.vertices)-2 for p in obj.data.polygons)
                  for obj in meshes)
    verts=[obj.matrix_world@v.co for obj in meshes for v in obj.data.vertices]
    mins=Vector([min(v[i] for v in verts) for i in range(3)])
    maxs=Vector([max(v[i] for v in verts) for i in range(3)])
    dim=maxs-mins
    rec={
       "file":file,"bytes":path.stat().st_size,"mesh_nodes":len(meshes),
       "materials":materials,"triangles":triangles,
       "placement_hints":hints,
       "world_dimensions_m":[round(v,4) for v in dim],
       "world_min_m":[round(v,4) for v in mins],
       "world_max_m":[round(v,4) for v in maxs]
    }
    assert len(meshes)<=12,rec
    assert len(hints)==4,rec
    assert 1000<triangles<20000,rec
    assert "COLUMN_DeepAzure" in materials,rec
    assert "STONE_LibrarySlate_AshGray" in materials,rec
    assert "ENAMEL_DeepNavyArch" in materials,rec
    assert 4.45<dim.x<5.50 and 4.15<dim.z<5.0,rec
    print('GLB_ROUNDTRIP_PASSED',json.dumps(rec))
    results.append(rec)
assert results[0]['triangles']>results[1]['triangles']>results[2]['triangles']
(QA/'GatewayGLBRoundtrip.json').write_text(json.dumps(results,indent=2))
