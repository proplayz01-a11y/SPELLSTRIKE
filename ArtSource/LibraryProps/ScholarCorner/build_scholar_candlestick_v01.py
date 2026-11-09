"""Standalone unlit ornate brass candleholder for the scholar side table."""
import bpy,json,math
from pathlib import Path
P=Path(__file__).resolve().parents[3]
SOURCE=Path(__file__).resolve().parent
EXPORT=P/'Assets/Cinematics/Storyboard3D/Environment/Props/ScholarCorner'
EXPORT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.render.engine='BLENDER_EEVEE'
def mat(n,c,metal,rough):
 m=bpy.data.materials.new(n);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF')
 bs.inputs['Base Color'].default_value=(*c,1)
 bs.inputs['Metallic'].default_value=metal
 bs.inputs['Roughness'].default_value=rough
 return m
brass=mat('ScholarCandlestick_AgedGold',(.48,.257,.07),.82,.34)
shiny=mat('ScholarCandlestick_BrassTrim',(.69,.45,.16),.80,.29)
wax=mat('ScholarCandlestick_CreamWax',(.8,.66,.46),0,.87)
charcoal=mat('ScholarCandlestick_CharcoalWick',(.04,.025,.018),0,.95)
collection=bpy.data.collections.new('SCHOLAR_CANDLESTICK_UNLIT')
scene.collection.children.link(collection)
root=bpy.data.objects.new('ScholarCandlestick_v01',None)
collection.objects.link(root)
root['unity_vfx_socket']='CandleFlame_ANCHOR'
root['model_contains_fire']=False
def cone(name,z,r0,r1,length,ma,segments=14):
 bpy.ops.mesh.primitive_cone_add(vertices=segments,radius1=r0,radius2=r1,depth=length,location=(0,0,z))
 ob=bpy.context.object
 for c in list(ob.users_collection):c.objects.unlink(ob)
 collection.objects.link(ob);ob.name=name;ob.parent=root
 ob.data.materials.append(ma)
 return ob
def torus(name,z,R,r,ma):
 bpy.ops.mesh.primitive_torus_add(major_radius=R,minor_radius=r,major_segments=22,minor_segments=5,location=(0,0,z))
 ob=bpy.context.object
 for c in list(ob.users_collection):c.objects.unlink(ob)
 collection.objects.link(ob);ob.name=name;ob.parent=root
 ob.data.materials.append(ma)
cone('BASE_AntiqueDisc',.018,.10,.075,.036,brass)
torus('BASE_GoldLipping',.030,.085,.007,shiny)
cone('STEM_CarvedBrassLower',.095,.047,.028,.12,brass)
cone('STEM_TaperedGoldUpper',.171,.028,.033,.060,shiny)
cone('CUP_CandleHolderSaucer',.215,.093,.053,.032,brass)
torus('CUP_OuterFineGoldRim',.230,.075,.007,shiny)
cone('WAX_ShortCreamCandle',.333,.039,.035,.185,wax)
torus('WAX_TopMeltedRim',.426,.031,.004,wax)
cone('WICK_Blackened',.441,.005,.002,.027,charcoal,8)
socket=bpy.data.objects.new('CandleFlame_ANCHOR',None)
collection.objects.link(socket);socket.parent=root
socket.location=(0,0,.470);socket.empty_display_size=.02
socket['attach']='Independent Unity flame VFX prefab'
scene.camera=None
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ScholarCandlestick_Unlit_v01.blend'),compress=True)
bpy.ops.object.select_all(action='DESELECT')
for ob in collection.objects:ob.select_set(True)
output=EXPORT/'ScholarCandlestick_Unlit_v01.glb'
bpy.ops.export_scene.gltf(filepath=str(output),export_format='GLB',use_selection=True,export_apply=True)
triangles=sum(sum(len(f.vertices)-2 for f in o.data.polygons)
              for o in collection.objects if o.type=='MESH')
print('CANDLESTICK_BUILT',json.dumps({'source':str(SOURCE/'ScholarCandlestick_Unlit_v01.blend'),'glb':str(output),'bytes':output.stat().st_size,'triangles':triangles,'flame':False,'root':root.name}))
