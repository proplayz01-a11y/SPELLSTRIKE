"""Independent round-trip validation for canonical study/archive Blender exports.

Checks the actual GLBs, not just the in-memory build report.
No Unity Editor or original Rodin files are changed.
"""
import bpy
import json
import zipfile
import math
from pathlib import Path
from mathutils import Vector

S=Path(__file__).resolve().parent
PROJECT=S.parents[2]
ART=PROJECT/"Assets/Cinematics/Storyboard3D/Environment/LibraryShell"
QA=PROJECT/"Library/StoryboardLibraryShell_20261022"

cases={
 "StoneWall_2m_Modular.glb":(1,660),
 "StoneFloor_Study_2m_Modular.glb":(1,192),
 "StoneFloor_Archive_2m_Modular.glb":(1,192),
 "Library_StoneWallsAndFloors_v01.glb":(200,None),
 "Library_ArchiveShelvesAndMarkers_v01.glb":(20,None),
 "Library_StudyFurnitureGatewayAndDecor_v01.glb":(20,None),
 "LibraryStudyArchive_OneBuilding_v01.glb":(250,None),
}

results={}
for name,(minimum_nodes,expected_tris) in cases.items():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    fp=ART/name
    assert fp.exists(),f'Missing {fp}'
    bpy.ops.import_scene.gltf(filepath=str(fp))
    meshes=[o for o in bpy.data.objects if o.type=='MESH']
    count=sum(sum(max(0,len(face.vertices)-2) for face in o.data.polygons)
              for o in meshes)
    names=[o.name for o in bpy.data.objects]
    materials={m.name for o in meshes for m in o.data.materials if m}
    imgs={n.image.name: list(n.image.size)
          for m in bpy.data.materials
          if m.use_nodes
          for n in m.node_tree.nodes
          if n.type=='TEX_IMAGE' and n.image}
    assert len(meshes)>=minimum_nodes,(name,len(meshes),minimum_nodes)
    assert count>0,(name,count)
    if expected_tris is not None:assert count==expected_tris,(name,count)
    rec={
      'bytes':fp.stat().st_size,
      'meshes':len(meshes),
      'triangles':count,
      'materials':len(materials),
      'images':len(imgs),
      'objects':len(names),
    }
    if name in ('Library_StudyFurnitureGatewayAndDecor_v01.glb',
                'LibraryStudyArchive_OneBuilding_v01.glb'):
        for side in (-1,1):
            banner=bpy.data.objects[f'STUDY_BANNER_{side}']
            front=banner.matrix_world.to_3x3() @ Vector((0,-1,0))
            assert front.y>.99,(name,banner.name,tuple(front))
            assert (banner.matrix_world.translation-Vector((side*1.65,-11.15,2.5))).length<.002
            assert len(banner.children)==6
        table=bpy.data.objects['STUDY_ScholarSideTable']
        globe=bpy.data.objects['STUDY_AntiqueCelestialGlobe']
        bpy.context.view_layer.update()
        assert globe.parent is table, 'GLB lost the table/globe parenting'
        assert (table.matrix_world.translation-Vector((5.32,-7.80,0))).length<.002
        assert (globe.matrix_world.translation-Vector((5.34,-8.11,.85))).length<.002
        assert abs(math.degrees(table.matrix_world.to_euler().z)+90)<.03
        rec['scholar_table_world_Z_deg']=-90.0
        rec['globe_moves_with_table']=True
    if name=='LibraryStudyArchive_OneBuilding_v01.glb':
        allnames=' '.join(names)
        for expected in ('WALL_DividerWest','WALL_DividerEast',
                         'ARCHIVE_InnerRack','STUDY_Shelf',
                         'CONTINUOUS_CrimsonRunner',
                         'GOTHIC_GATEWAY','STUDY_MainStudyTable',
                         'STUDY_ScholarSideTable','STUDY_AntiqueCelestialGlobe',
                         'SOUTH_GlassAmberPane'):
            assert expected in allnames,(expected,'missing in assembled GLB')
        # Individually hidden prototype wall at x=0..2 is not exported.
        assert not any(n.startswith('Wall_SlateStraight_') for n in names),(
            'Template meshes must not obstruct the actual gateway',)
        rec['contains_continuous_runner']=True
        rec['source_template_wall_absent']=True
    results[name]=rec
    print('GLB_REIMPORT_OK',name,json.dumps(rec))

assert (results['LibraryStudyArchive_OneBuilding_v01.glb']['triangles']==
        results['Library_StoneWallsAndFloors_v01.glb']['triangles']+
        results['Library_ArchiveShelvesAndMarkers_v01.glb']['triangles']+
        results['Library_StudyFurnitureGatewayAndDecor_v01.glb']['triangles'])

manifest=QA/'LibraryStudyArchive_v01_GLBRoundtrip.json'
manifest.write_text(json.dumps(results,indent=2),encoding='utf-8')
print('QA_COMPLETE',str(manifest))
