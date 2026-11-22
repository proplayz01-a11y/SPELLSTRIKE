"""Read-only round-trip reimport of standalone glTF skinned character prototypes."""
import bpy,json
from pathlib import Path

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
ASSET=ROOT/'Assets/Cinematics/Storyboard3D/Characters/DefensePrototypes'
QA=ROOT/'Library/StoryboardDefenseCharacters_20261023'
SPECS=[
 ('YoungVanAert_Prototype_v01','YoungVan_WatchingDaughter_Idle_v01',
  ['FACE_CleanShaven','BlackHairCap','RoundGoldChestClasp']),
 ('Nevali_Prototype_v01','Nevali_ReachingPlayingToyBlocks_v01',
  ['NEVALI_CREAM_DRESS_SKIRT','NEVALI_BLUE_BODICE','NEVALI_CHESTNUT_HAIR_CAP'])
]
entries=[]
for name,action_name,required in SPECS:
 glb=ASSET/(name+'.glb')
 blend=HERE/(name+'.blend')
 png=QA/(name+'_preview.png')
 assert glb.is_file() and blend.is_file() and png.is_file(),name
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.gltf(filepath=str(glb))
 objs=list(bpy.data.objects)
 names={o.name for o in objs}
 arms=[o for o in objs if o.type=='ARMATURE']
 assert len(arms)==1,(name,'armatures',len(arms))
 assert len(arms[0].data.bones)==18,(name,'bones',len(arms[0].data.bones))
 mesh=[o for o in objs if o.type=='MESH']
 assert len(mesh)>35,(name,'meshes',len(mesh))
 missing=[k for k in required if k not in names]
 assert not missing,(name,missing)
 skins=sum(bool(o.find_armature()) for o in mesh)
 assert skins>=len(mesh)-2,(name,'not skinned',len(mesh),skins)
 assert len(bpy.data.actions)>=1,(name,'animations')
 acts=[a.name for a in bpy.data.actions]
 assert any(action_name in a for a in acts),(name,'missing action',acts)
 tris=sum(sum(max(0,len(poly.vertices)-2) for poly in o.data.polygons) for o in mesh)
 # Blender's importer can create an unskinned Icosphere helper (no
 # glTF mesh node/material loss). Validate the actual skinned actor parts.
 null_materials=[o.name for o in mesh if o.find_armature() and
                 len(o.data.materials)==0]
 assert not null_materials,(name,null_materials)
 entries.append({'name':name,'blend_bytes':blend.stat().st_size,
    'glb_bytes':glb.stat().st_size,'meshes':len(mesh),'triangles':tris,
    'bones':len(arms[0].data.bones),'skinned_meshes':skins,
    'animations':acts,'missing_expected_parts':missing,
    'null_material_meshes':null_materials,'pass':True})
QA.mkdir(parents=True,exist_ok=True)
result={'all_pass':len(entries)==2,'status':'RIGGED_DEFENSE_PROTOTYPE_NOT_FINAL_HUMANOID_MOCAP',
        'unity_scene_actor_placement_verified':False,'characters':entries}
(QA/'CharacterPrototypeRoundtripQA.json').write_text(
 json.dumps(result,indent=2),encoding='utf8')
print('TWO_SKINNED_CHARACTERS_ROUNDTRIP_PASS',json.dumps(entries))
