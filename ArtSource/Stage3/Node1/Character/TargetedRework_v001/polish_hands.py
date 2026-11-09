import bpy,pathlib,math,bmesh
from mathutils import Vector
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');bpy.ops.wm.open_mainfile(filepath=str(O/'SunboundWarden_TargetedRework_v001.blend'));sc=bpy.context.scene;coll=bpy.data.collections['REWORK_New_Hands'];gold=bpy.data.materials['Warden_Gauntlet_AntiqueGold'];edge=bpy.data.materials['Warden_Gauntlet_EdgeGold'];glove=bpy.data.materials['Warden_Glove_DeepPurple']
# Replace draft box plates with curved, anatomical metal shells.
for o in list(coll.objects):
 if any(k in o.name for k in ['Plate_','Backplate','SunBoss','SunRay']):bpy.data.objects.remove(o,do_unlink=True)
def meshobj(name,vs,fs,mat):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);coll.objects.link(o);me.materials.append(mat)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 return o
def shell(name,vs,fs,mat,thick=.0016):
 o=meshobj(name,vs,fs,mat);s=o.modifiers.new('Armor thickness','SOLIDIFY');s.thickness=thick;s.offset=1;b=o.modifiers.new('Soft forged edge','BEVEL');b.width=.0007;b.segments=2;o.modifiers.new('Weighted highlights','WEIGHTED_NORMAL');return o
# Cover the seam inside the existing cuff with a shaped sleeve, not a flat cut surface.
vs=[];fs=[];n=24
for x,ry,rz in [(.792,.069,.046),(.799,.068,.044),(.808,.065,.041),(.817,.053,.026),(.824,.050,.024)]:
 for i in range(n):
  a=i*math.tau/n;vs.append((x,.084+ry*math.cos(a),1.290+rz*math.sin(a)))
for row in range(4):
 for i in range(n):fs.append((row*n+i,row*n+(i+1)%n,(row+1)*n+(i+1)%n,(row+1)*n+i))
sleeve=shell('Warden_Hand_R_WristSeal',vs,fs,glove,.001)
for p in sleeve.data.polygons:p.use_smooth=True
# Small metal cuff lip follows elliptical sleeve.
vs=[];fs=[]
for x in [.805,.8085]:
 for i in range(n):
  a=i*math.tau/n;vs.append((x,.084+.0665*math.cos(a),1.290+.0425*math.sin(a)))
for i in range(n):fs.append((i,(i+1)%n,n+(i+1)%n,n+i))
shell('Warden_Hand_R_WristLip',vs,fs,edge,.0015)
# Tapered dorsal plate follows knuckle fan; center ridge and inset face.
outline=[(.815,.056),(.823,.044),(.851,.047),(.864,.060),(.868,.083),(.864,.108),(.851,.121),(.823,.124),(.815,.112)]
def shield(name,scale,z,mat):
 pts=[(.839+(x-.839)*scale,.084+(y-.084)*scale,z) for x,y in outline];vs=pts+[(.839,.084,z+.004)];fs=[(i,(i+1)%len(pts),len(pts)) for i in range(len(pts))];return shell(name,vs,fs,mat,.0018)
shield('Warden_Hand_R_BackplateBorder',1,1.316,edge);shield('Warden_Hand_R_BackplateInset',.90,1.318,gold)
for name,length,y,splay,r in [('Little',.061,.044,-.018,.010),('Ring',.078,.0705,-.007,.0105),('Middle',.088,.098,.002,.011),('Index',.081,.125,.012,.010)]:
 for k,(a,b) in enumerate([(.03,.32),(.44,.67),(.79,.99)]):
  vs=[];fs=[];steps=[a,a+(b-a)*.14,(a+b)/2,b-(b-a)*.1,b]
  for t in steps:
   x=.872+length*t;cy=y+splay*t;cz=1.290-.017*t*t;factor=1-.36*t;ry=r*1.02*factor;rz=(.014+.007*max(0,1-t/.34))*factor
   for deg in [-80,-45,0,45,80]:
    ang=math.radians(deg);vs.append((x,cy+ry*math.sin(ang),cz+rz*math.cos(ang)+.0012))
  for u in range(4):
   for j in range(4):fs.append((u*5+j,(u+1)*5+j,(u+1)*5+j+1,u*5+j+1))
  o=shell('Warden_Hand_R_'+name+'_CurvedPlate_'+str(k+1),vs,fs,gold)
  for p in o.data.polygons:p.use_smooth=True
# Thumb armor wraps its diagonal axis.
points=[Vector((.833,.146,1.285)),Vector((.848,.165,1.278)),Vector((.861,.180,1.269)),Vector((.879,.192,1.262)),Vector((.891,.197,1.260))]
for k in range(3):
 a=points[k];b=points[k+1];d=(b-a).normalized();u=Vector((-d.y,d.x,0)).normalized();vs=[];fs=[]
 for t in [.10,.25,.75,.90]:
  p=a.lerp(b,t);r=.013-k*.0015
  for deg in [-75,-40,0,40,75]:
   an=math.radians(deg);vs.append(tuple(p+u*(math.sin(an)*r)+Vector((0,0,math.cos(an)*r+.001))))
 for i in range(3):
  for j in range(4):fs.append((i*5+j,(i+1)*5+j,(i+1)*5+j+1,i*5+j+1))
 o=shell('Warden_Hand_R_Thumb_CurvedPlate_'+str(k+1),vs,fs,gold)
 for p in o.data.polygons:p.use_smooth=True
# Solar boss and fine rays, smaller than the draft motif.
def tube(name,pts,r,mat):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2;s=cu.splines.new('POLY');s.points.add(len(pts)-1)
 for p,co in zip(s.points,pts):p.co=(*co,1)
 ob=bpy.data.objects.new(name,cu);coll.objects.link(ob);cu.materials.append(mat);return ob
for rad,z in [(.009,1.326),(.011,1.325)]:tube('Warden_Hand_R_SunRing',[(.839+rad*math.cos(i*math.tau/32),.084+rad*math.sin(i*math.tau/32),z) for i in range(33)],.0012,edge)
for i in range(8):
 a=i*math.tau/8;tube('Warden_Hand_R_SunRay',[(.839+math.cos(a)*r,.084+math.sin(a)*r,1.325) for r in [.015,.021]],.0012,edge)
# Subtle stitched seams on palm, designed as part of the glove.
dark=bpy.data.materials.get('Warden_Glove_Seams') or bpy.data.materials.new('Warden_Glove_Seams');dark.diffuse_color=(.04,.025,.052,1)
tube('Warden_Hand_R_PalmSeam',[(.817,.049,1.269),(.835,.046,1.268),(.849,.054,1.268),(.856,.079,1.268),(.855,.114,1.270)],.00055,dark)
# Mirror newly authored right parts. The glove left/right already exist.
for o in list(coll.objects):
 if '_R_' in o.name and 'Glove' not in o.name:
  c=o.copy();c.data=o.data.copy();coll.objects.link(c);c.name=o.name.replace('_R_','_L_');c.location.x=.002138;c.scale.x=-1
# New materials have restrained roughness variation rather than featureless plastic.
for m in [gold,edge,glove]:
 nt=m.node_tree;p=nt.nodes.get('Principled BSDF');noise=nt.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=140;noise.inputs['Detail'].default_value=2;r=nt.nodes.new('ShaderNodeMapRange');r.inputs['From Min'].default_value=0;r.inputs['From Max'].default_value=1;r.inputs['To Min'].default_value=.32 if m!=glove else .55;r.inputs['To Max'].default_value=.48 if m!=glove else .72;nt.links.new(noise.outputs['Fac'],r.inputs['Value']);nt.links.new(r.outputs['Result'],p.inputs['Roughness'])
cam=sc.camera;cam.data.ortho_scale=2.18;center=Vector((0,0,.847));cam.location=center+Vector((0,-5,0));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.select_all(action='DESELECT');bpy.data.objects['SunboundWarden_Body_Armor_Preserved'].select_set(True);bpy.context.view_layer.objects.active=bpy.data.objects['SunboundWarden_Body_Armor_Preserved']
bpy.ops.wm.save_as_mainfile(filepath=str(O/'SunboundWarden_TargetedRework_v002.blend'))
sc.render.filepath=str(O/'Final_Front.png');bpy.ops.render.render(write_still=True)
cam.data.ortho_scale=.32;center=Vector((.868,.105,1.292))
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandFront',(0,-2,.35)),('HandOblique',(1,-1,2))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(O/('Final_'+name+'.png'));bpy.ops.render.render(write_still=True)


