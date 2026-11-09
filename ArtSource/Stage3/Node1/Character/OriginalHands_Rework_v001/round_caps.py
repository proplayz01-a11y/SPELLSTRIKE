import bpy,pathlib,json,math
from mathutils import Vector
from mathutils.kdtree import KDTree
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/OriginalHands_Rework_v001');bpy.ops.wm.open_mainfile(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v001.blend'));sc=bpy.context.scene;o=bpy.data.objects['SunboundWarden_Original_RefinedHands'];me=o.data;original=[Vector(p) for p in json.loads((OUT/'source_geometry.json').read_text())['vertices']];groups=json.loads((OUT/'finger_components.json').read_text())['0.855']
def cap(ids,axis,end_retract,depth):
 axis=Vector(axis).normalized();u=Vector((-axis.y,axis.x,0)).normalized();z=Vector((0,0,1));pts=[me.vertices[i].co.copy() for i in ids];ss=[p.dot(axis) for p in pts];mx=max(ss);mn=min(ss);cut=mx-.014
 tail=[i for i in ids if me.vertices[i].co.dot(axis)>cut];c=sum((me.vertices[i].co for i in tail),Vector())/len(tail);cu=c.dot(u);cz=c.z
 rawu=max(abs(me.vertices[i].co.dot(u)-cu) for i in tail);rawz=max(abs(me.vertices[i].co.z-cz) for i in tail);ru=max(.0048,rawu*.92);rz=max(.0050,rawz*.92)
 for i in tail:
  p=me.vertices[i].co.copy();s=p.dot(axis);t=max(0,min(1,(s-cut)/.014));w=t*t*(3-2*t);du=(p.dot(u)-cu);dz=p.z-cz;r=math.sqrt((du/max(rawu,.0001))**2+(dz/max(rawz,.0001))**2);r=min(.985,r)
  angle=math.atan2(dz/max(rawz,.0001),du/max(rawu,.0001));uu=cu+math.cos(angle)*ru*r;zz=cz+math.sin(angle)*rz*r;xx=mx-end_retract-depth*(1-math.sqrt(max(0,1-r*r)));target=axis*xx+u*uu+z*zz;me.vertices[i].co=p.lerp(target,w)
for p in groups:cap(p['ids'],(1,0,0),.0022,.0052)
thumb=[v.index for v in me.vertices if v.co.x>.814 and v.co.y>.137 and v.co.x<.89 and 1.24<v.co.z<1.31];cap(thumb,(.78,.625,0),.0014,.005)
# Restore matched left-handed edits from the same original correspondences.
right=[i for i,p in enumerate(original) if p.x>.79 and p.z>1.2];kd=KDTree(len(right))
for i in right:kd.insert(original[i],i)
kd.balance()
for i,p in enumerate(original):
 if p.x<-.78 and p.z>1.2:
  q,idx,d=kd.find(Vector((.002138-p.x,p.y,p.z)))
  if d<.00002:
   delta=me.vertices[idx].co-original[idx];me.vertices[i].co=p+Vector((-delta.x,delta.y,delta.z))
me.update();o.modifiers['Rework - soften original fingertip corners'].width=.0022
qa=json.loads((OUT/'geometry_qa.json').read_text());qa['final_tip_pass']='Rounded existing distal vertices with 2.2mm apex retraction and tunable 2.2mm vertex bevel; source UVs unchanged';qa['final_base_vertex_count']=len(me.vertices);qa['final_base_face_count']=len(me.polygons);qa['body_coordinates_unchanged']=all(v.co==p for v,p in zip(me.vertices,original) if abs(p.x-.001069)<.79)
qa['final_max_x']={label:max(me.vertices[i].co.x for i in p['ids']) for label,p in zip(['Little','Ring','Middle','Index'],groups)};(OUT/'geometry_qa_final.json').write_text(json.dumps(qa,indent=2))
centerbody=Vector((.001,0,.847));cam=sc.camera;cam.data.ortho_scale=2.17;cam.location=centerbody+Vector((0,-4,0));cam.rotation_euler=(centerbody-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v002.blend'))
sc.render.filepath=str(OUT/'Final_Full_Front.png');bpy.ops.render.render(write_still=True)
center=Vector((.866,.088,1.286));cam.data.ortho_scale=.315
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandOblique',(1,-1,2)),('HandFront',(0,-2,.4))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('Final_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('FINAL_CAP_PASS',qa['final_max_x'])
