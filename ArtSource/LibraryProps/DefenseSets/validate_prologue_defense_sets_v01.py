"""Re-import the three Blender GLBs to catch broken exports before Unity."""
import bpy
import json
from pathlib import Path

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parents[2]
ASSETS=PROJECT/'Assets/Cinematics/Storyboard3D/Environment/DefenseSets'
QA=PROJECT/'Library/StoryboardDefenseSets_20261023'
SPECS={
    'Prologue_Home_01_Peaceful_v01': [
        'TOY_TOWER_BLOCK_00','FAMILY_TABLE',
        'ANCHOR_VAN_YOUNG','ANCHOR_NEVALI_CHILD','DOOR_ENTRY_HEADER'],
    'Prologue_GreatWar_02_03_v01': [
        'WORD_MAGIC_TILE_A','WORD_MAGIC_LETTER_A','WORD_MAGIC_LETTER_G',
        'ENTITY_ABSTRACT_TORSO_PROXY','ANCHOR_VAN_WAR_FIGHT'],
    'Prologue_Home_04_Ruined_v01': [
        'NEVALI_BLUE_RIBBON_EVIDENCE','RUIN_SINGED_RUG',
        'BROKEN_WOOD_PLANK_00','ANCHOR_VAN_KNEEL']
}
checks=[]
for stem,expected in SPECS.items():
    path=ASSETS/(stem+'.glb')
    source=HERE/(stem+'.blend')
    png=QA/(stem+'_preview.png')
    assert path.is_file() and source.is_file() and png.is_file(),stem
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(path))
    objs=list(bpy.data.objects)
    names={o.name for o in objs}
    missing=[x for x in expected if x not in names]
    assert not missing,(stem,'missing',missing)
    mesh=[o for o in objs if o.type=='MESH']
    assert len(mesh)>20,(stem,'too few meshes',len(mesh))
    tris=sum(sum(max(0,len(face.vertices)-2) for face in o.data.polygons) for o in mesh)
    assert tris>=1000,(stem,'too few triangles',tris)
    broken=[o.name for o in mesh if o.data is None or len(o.data.vertices)==0]
    assert not broken,(stem,broken)
    matless=[o.name for o in mesh if len(o.data.materials)==0]
    assert not matless,(stem,'material missing',matless[:12])
    checks.append({'name':stem,'source_blend_bytes':source.stat().st_size,
        'glb_bytes':path.stat().st_size,'preview_png_bytes':png.stat().st_size,
        'roundtrip_mesh_count':len(mesh),'roundtrip_triangles':tris,
        'required_named_items':expected,'materials':len(bpy.data.materials),
        'missing_required':missing,'pass':True})
report={'test':'Blender glTF reimport round-trip, authored physical meshes',
        'all_pass':len(checks)==3 and all(v['pass'] for v in checks),
        'actor_rigs_included':False,
        'unity_editor_scenes_verified':False,
        'checks':checks}
QA.mkdir(parents=True,exist_ok=True)
(QA/'PrototypeEnvironmentRoundtripQA.json').write_text(
    json.dumps(report,indent=2),encoding='utf8')
print('DEFENSE_ENVIRONMENT_ROUNDTRIP_PASS '+json.dumps(report))
