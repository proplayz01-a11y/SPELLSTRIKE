import bpy,pathlib,math,bmesh
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/NewMesh_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Armor_v001.blend'));sc=bpy.context.scene;ARMOR=bpy.data.collections['LAYER_2_Armor'];DETAIL=bpy.data.collections['LAYER_2_Armor_Detail'];CLOTH=bpy.data.collections['LAYER_2_Cloth'];CORE=bpy.data.collections['LAYER_1_Anatomical_Core'];REVIEW=bpy.data.collections['REVIEW_Studio']
Gold=bpy.data.materials['Warden_Antique_Gold'];LightGold=bpy.data.materials['Warden_Burnished_Edges'];DarkGold=bpy.data.materials['Warden_Recess_Bronze'];Purple=bpy.data.materials['Warden_Royal_Purple'];Leather=bpy.data.materials['Warden_Dark_Leather'];Dark=bpy.data.materials['Warden_Shadow_Recess'];Silver=bpy.data.materials['Warden_Warm_Steel']
# Reuse only mesh construction helpers, never source character geometry.
code=(OUT/'build_new_mesh.py').read_text();exec(code[code.index('def objmesh'):code.index('# Muscle/garment')])
code=(OUT/'build_armor.py').read_text();exec(code[code.index('def place'):code.index('# Fill all visible')])
# Give chest, cheek, hip and shin plates a raised center and bevel-friendly facets.
for o in list(ARMOR.objects):
 if o.type!='MESH' or len(o.data.polygons)!=1:continue
 if any(k in o.name for k in ['Pectoral','Cheek','Tasset','Greave','Knee','Bracer_Dorsal','Abdominal']):
  me=o.data;coords=[v.co.copy() for v in me.vertices];n=len(coords);center=sum(coords,Vector())/n
  if 'Bracer_Dorsal' in o.name:center.z+=.010
  else:center.y-=.012 if center.y<0 else -.012
  me.clear_geometry();me.from_pydata(coords+[center],[],[(i,(i+1)%n,n) for i in range(n)]);me.update()
# Replace ball-like shoulder appearance with fitted, articulated shell skirts.
for o in ARMOR.objects:
 if 'Shoulder_Under' in o.name:o.data.materials[0]=Leather
for side in [1,-1]:
 for front in [True,False]:
  sg=-1 if front else 1
  for row in range(3):
   top=1.490-row*.037;bottom=top-.057;vs=[];fs=[]
   for r,z in enumerate([top,(top+bottom)/2,bottom]):
    width=.104-r*.007-row*.006
    for k in range(9):
     u=k/8*2-1;x=side*(.302+u*width);yy=sg*(.119-.016*u*u);zz=z+.023*u*u;vs.append((x,yy,zz))
   for r in range(2):
    for k in range(8):a=r*9+k;fs.append((a,a+1,a+10,a+9))
   o=objmesh('Pauldron_Skirt_'+str(side)+'_'+str(front)+'_'+str(row),vs,fs,Gold,ARMOR,True);s=o.modifiers.new('Armor volume','SOLIDIFY');s.thickness=.005;b=o.modifiers.new('Rolled lip','BEVEL');b.width=.0018;b.segments=2
   curve('Pauldron_Skirt_Trim',[vs[18+i] for i in range(9)],.0023,LightGold)
# Finely fluted horn channels and brow extensions.
for side in [1,-1]:
 curve('Horn_Outer_Groove',[(side*.11,-.018,1.766),(side*.153,-.019,1.835),(side*.151,-.012,1.895),(side*.133,-.008,1.96)],.0015,DarkGold)
 curve('Helmet_Brow_Inlay',[(side*.018,-.126,1.765),(side*.052,-.116,1.774),(side*.083,-.095,1.767)],.0025,DarkGold)
 # Shoulder badge uses engraved concentric sun arcs.
 for r in [.020,.028]:curve('Pauldron_Arc',[(side*(.309+math.sin(a)*r),-.136,1.516+math.cos(a)*r*.65) for a in [i*math.pi/12 for i in range(25)]],.0015,LightGold)
 # Chest side flange and vent slits.
 for i in range(3):
  curve('Cu irass_Side_Vent',[(side*(.181+i*.009),-.086,1.175),(side*(.188+i*.009),-.077,1.225)],.0017,DarkGold)
# Pointed cloth boundaries and consistent embroidered edging.
for o in CLOTH.objects:
 if 'Tabard' in o.name or 'Banner' in o.name:
  attr=o.data.attributes.get('crease_edge') or o.data.attributes.new('crease_edge','FLOAT','EDGE');bm=bmesh.new();bm.from_mesh(o.data);boundary={e.index for e in bm.edges if e.is_boundary};bm.free()
  for i,d in enumerate(attr.data):d.value=1.0 if i in boundary else 0
# Fine textile texture on cloth and leather, without borrowed painted source maps.
for mat in [Purple,Leather]:
 nt=mat.node_tree;p=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=220;tex.inputs['Detail'].default_value=2;b=nt.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.13;b.inputs['Distance'].default_value=.0005;nt.links.new(tex.outputs['Fac'],b.inputs['Height']);nt.links.new(b.outputs['Normal'],p.inputs['Normal'])
# Subtle fine metal wear and etched bevel relief.
for mat in [Gold,LightGold,DarkGold]:
 nt=mat.node_tree;p=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=180;tex.inputs['Detail'].default_value=2;b=nt.nodes.new('ShaderNodeBump');b.inputs['Strength'].default_value=.11;b.inputs['Distance'].default_value=.0004;nt.links.new(tex.outputs['Fac'],b.inputs['Height']);nt.links.new(b.outputs['Normal'],p.inputs['Normal'])
# Normalize the new character, horns included, to 1.90 m; all dimensions are new.
objs=list(CORE.objects)+list(ARMOR.objects)+list(CLOTH.objects)+list(DETAIL.objects);dg=bpy.context.evaluated_depsgraph_get();verts=[]
for o in objs:
 e=o.evaluated_get(dg);me=e.to_mesh();verts.extend(o.matrix_world@v.co for v in me.vertices);e.to_mesh_clear()
zlo=min(v.z for v in verts);zhi=max(v.z for v in verts);factor=1.90/(zhi-zlo)
for o in objs:
 o.location*=factor;o.location.z-=zlo*factor;o.scale*=factor
# Render studio: broad area lights and a neutral floor.
def light(name,pos,power,size,color):
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color;o=bpy.data.objects.new(name,d);REVIEW.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.95))-o.location).to_track_quat('-Z','Y').to_euler();return o
light('Key_Softbox',(2.8,-3.8,4.0),600,3,(1,.91,.79));light('Fill_Softbox',(-3,-1.7,2.4),420,3,(.73,.82,1));light('Rim_Softbox',(1,2.8,3.0),650,2,(1,.85,.62))
floor=bpy.data.materials.new('Review_Floor');floor.diffuse_color=(.032,.036,.05,1);floor.use_nodes=True;floor.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.032,.036,.05,1);floor.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.83
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.003));o=bpy.context.object;o.name='Review_Floor';place(o,REVIEW);o.data.materials.append(floor)
sc.world.use_nodes=True;sc.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.13,.15,.20,1);sc.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.35
sc.view_settings.view_transform='AgX';sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True;sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
cam=sc.camera;cam.location=(3,-5,2.0);cam.rotation_euler=(Vector((0,0,.94))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.35
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='MATERIAL';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_NewMesh_Refined.blend'))
sc.render.filepath=str(OUT/'NewMesh_ThreeQuarter.png');bpy.ops.render.render(write_still=True)
