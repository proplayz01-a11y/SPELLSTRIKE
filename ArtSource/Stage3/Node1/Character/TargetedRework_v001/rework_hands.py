import bpy,bmesh,math,pathlib,json
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');bpy.ops.wm.open_mainfile(filepath=str(O/'Source_Inspection.blend'));sc=bpy.context.scene;src=bpy.data.objects['model']
ref=bpy.data.collections.new('REFERENCE_Original_Unedited');sc.collection.children.link(ref)
for c in list(src.users_collection):c.objects.unlink(src)
ref.objects.link(src);src.hide_render=True;src.hide_set(True);ref.hide_render=True
body=src.copy();body.data=src.data.copy();sc.collection.objects.link(body);body.name='SunboundWarden_Body_Armor_Preserved';body.hide_render=False;body.hide_set(False)
bm=bmesh.new();bm.from_mesh(body.data)
for x,normal in [(.795,(1,0,0)),(-.792862,(-1,0,0))]:
 ret=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0000001,plane_co=(x,0,0),plane_no=normal,clear_outer=True,clear_inner=False)
 edges=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary]
 if edges:bmesh.ops.holes_fill(bm,edges=edges,sides=0)
bm.to_mesh(body.data);bm.free();body.data.update()
coll=bpy.data.collections.new('REWORK_New_Hands');sc.collection.children.link(coll)
def mat(name,color,metal=0,rough=.5):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough;return m
glove=mat('Warden_Glove_DeepPurple',(.095,.052,.14),0,.65);gold=mat('Warden_Gauntlet_AntiqueGold',(.48,.355,.195),.7,.38);edge=mat('Warden_Gauntlet_EdgeGold',(.67,.51,.30),.75,.32);dark=mat('Warden_Glove_Seams',(.04,.025,.052),0,.8)
def link_obj(name,mesh,material):
 o=bpy.data.objects.new(name,mesh);coll.objects.link(o);o.data.materials.append(material)
 for p in mesh.polygons:p.use_smooth=True
 return o
# Hand glove: connected quad palm, four branched fingers, opposed thumb.
verts=[];faces=[]
def v(p):verts.append(tuple(p));return len(verts)-1
def face(ids):faces.append(tuple(ids))
ys=[-.050,-.030,-.024,-.003,.003,.025,.031,.051];xs=[.797,.815,.842,.872];cy=.084;cz=1.290
rings=[]
for row,x in enumerate(xs):
 w=[.72,.85,1,1][row];top=cz+[.017,.019,.021,.018][row];bot=cz-[.019,.022,.022,.017][row]
 rings.append([[v((x,cy+y*w,top)) for y in ys],[v((x,cy+y*w,bot)) for y in ys]])
for i in range(3):
 for j in range(7):face([rings[i][0][j],rings[i+1][0][j],rings[i+1][0][j+1],rings[i][0][j+1]]);face([rings[i][1][j+1],rings[i+1][1][j+1],rings[i+1][1][j],rings[i][1][j]])
 face([rings[i][0][0],rings[i][1][0],rings[i+1][1][0],rings[i+1][0][0]])
 if i!=1:face([rings[i][0][-1],rings[i+1][0][-1],rings[i+1][1][-1],rings[i][1][-1]])
for j in range(7):face([rings[0][0][j+1],rings[0][1][j+1],rings[0][1][j],rings[0][0][j]])
for j in [1,3,5]:face([rings[-1][0][j],rings[-1][1][j],rings[-1][1][j+1],rings[-1][0][j+1]])
fingerdata=[]
for idx,(name,length,splay) in enumerate([('Little',.061,-.018),('Ring',.078,-.007),('Middle',.088,.002),('Index',.081,.012)]):
 j=idx*2;y=cy+(ys[j]+ys[j+1])/2;r=(ys[j+1]-ys[j])/2
 prev=[rings[-1][0][j],rings[-1][0][j+1],rings[-1][1][j+1],rings[-1][1][j]]
 samples=[]
 for t,factor in [(0,1),(.08,1),(.32,.94),(.37,.97),(.43,.87),(.66,.78),(.71,.83),(.78,.70),(.94,.62),(1,.35)]:
  x=.872+length*t;yy=y+splay*t;z=cz-.017*t*t;ry=r*factor;rz=(.0135 if t>.1 else .017)*factor
  samples.append((t,Vector((x,yy,z)),ry,rz))
  if t==0:continue
  cur=[v((x,yy-ry,z+rz)),v((x,yy+ry,z+rz)),v((x,yy+ry,z-rz)),v((x,yy-ry,z-rz))]
  for k in range(4):face([prev[k],cur[k],cur[(k+1)%4],prev[(k+1)%4]])
  prev=cur
 face(prev);fingerdata.append((name,length,y,splay,r))
# Thumb exits radial side of palm through an actual shared opening.
prev=[rings[1][0][-1],rings[2][0][-1],rings[2][1][-1],rings[1][1][-1]]
thumbpoints=[Vector((.833,.146,1.285)),Vector((.848,.165,1.278)),Vector((.861,.180,1.269)),Vector((.879,.192,1.262)),Vector((.891,.197,1.260))]
for i,p in enumerate(thumbpoints):
 rr=[.013,.012,.011,.009,.0045][i];u=Vector((.78,-.625,0));z=Vector((0,0,rr*.95));cur=[v(p-u*rr+z),v(p+u*rr+z),v(p+u*rr-z),v(p-u*rr-z)]
 for k in range(4):face([prev[k],cur[k],cur[(k+1)%4],prev[(k+1)%4]])
 prev=cur
face(prev)
me=bpy.data.meshes.new('Hand_Continuous_QuadBase');me.from_pydata(verts,[],faces);me.update();hand=link_obj('Warden_Hand_R_Glove',me,glove)
bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
sub=hand.modifiers.new('Rounded glove surface','SUBSURF');sub.levels=2;sub.render_levels=2
# Dorsal armor panels use beveled solids, separate from flexible glove.
def plate(name,center,scale,material=gold,rot=(0,0,0),bevel=.002):
 bpy.ops.mesh.primitive_cube_add(size=1,location=center);o=bpy.context.object;o.name=name;o.scale=scale;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.rotation_euler=rot
 for c in list(o.users_collection):c.objects.unlink(o)
 coll.objects.link(o);o.data.materials.append(material);b=o.modifiers.new('Forged rounded edges','BEVEL');b.width=bevel;b.segments=2;b.affect='EDGES';o.modifiers.new('Weighted plate normals','WEIGHTED_NORMAL');return o
plate('Warden_Hand_R_Backplate_Trim',(.838,.084,1.309),(.063,.076,.007),edge,bevel=.007)
plate('Warden_Hand_R_Backplate',(.838,.084,1.313),(.055,.068,.007),gold,bevel=.006)
for name,length,y,splay,r in fingerdata:
 for k,(a,b) in enumerate([(0.025,.31),(.45,.65),(.80,.98)]):
  t=(a+b)/2;pos=(.872+length*t,y+splay*t,cz-.017*t*t+.0105*(1-.35*t));angle=math.atan2(splay,length);pitch=math.atan(.034*t/length)
  plate('Warden_Hand_R_'+name+'_Plate_'+str(k+1),pos,(length*(b-a),r*1.62*(1-.32*t),.008),gold,rot=(0,pitch,angle),bevel=.0022)
for i in range(3):
 a=thumbpoints[i];b=thumbpoints[i+1];p=(a+b)/2;p.z+=.010;d=b-a
 plate('Warden_Hand_R_Thumb_Plate_'+str(i+1),p,(d.length*.82,.016-i*.0015,.007),gold,rot=(0,-math.atan2(d.z,math.hypot(d.x,d.y)),math.atan2(d.y,d.x)),bevel=.0025)
# Small solar motif ties the rebuilt hand to the chest/knee language.
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=(.838,.084,1.320));o=bpy.context.object;o.name='Warden_Hand_R_SunBoss';o.scale=(.010,.010,.004)
for c in list(o.users_collection):c.objects.unlink(o)
coll.objects.link(o);o.data.materials.append(edge)
for i in range(8):
 a=i*math.tau/8;plate('Warden_Hand_R_SunRay_%02d'%i,(.838+math.cos(a)*.017,.084+math.sin(a)*.017,1.319),(.009,.003,.003),edge,rot=(0,0,a),bevel=.0007)
# Mirror rebuilt right hand geometry, preserving source center offset.
right=list(coll.objects)
for o in right:
 c=o.copy();c.data=o.data.copy();coll.objects.link(c);c.name=o.name.replace('_R_','_L_');c.matrix_world=o.matrix_world.copy();c.location.x=.002138-o.location.x;c.scale.x*=-1
 if o==hand:
  # Glove vertices use world-authored coordinates; reflect whole object around source symmetry plane.
  c.location.x=.002138
# Object roots/collections make source and rework inspectable.
for o in coll.objects:o['rework_scope']='New hand geometry; original body armor retained'
body['preserved_region']='Source geometry inside wrist planes; original UVs/material preserved'
sc.frame_start=1;sc.frame_end=1;sc.frame_set(1)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'

for img in bpy.data.images:
 if img.has_data and not img.packed_file:img.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_TargetedRework_v001.blend'))
cam=sc.camera;cam.data.ortho_scale=.32;center=Vector((.868,.105,1.292))
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandFront',(0,-2,.35)),('HandOblique',(1,-1,2))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(O/('Rework_'+name+'.png'));bpy.ops.render.render(write_still=True)
cam.data.ortho_scale=2.18;center=Vector((0,0,.847));cam.location=center+Vector((0,-5,0));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(O/'Rework_Front.png');bpy.ops.render.render(write_still=True)
print('REWORK_SAVED')


