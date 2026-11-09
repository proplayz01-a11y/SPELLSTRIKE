import bpy,bmesh,pathlib,json,math,hashlib
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
import numpy as np
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001');SOURCE=pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend');bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;o=bpy.data.objects['model'];src=o.data;src.calc_loop_triangles();original_coords=[v.co.copy() for v in src.vertices];uvsrc=src.uv_layers.active.data
# Digit-specific projection cages retain the original painted material/UV character.
def projector(side,label,cy):
 ts=[];uvs=[];points=[]
 for t in src.loop_triangles:
  ps=[src.vertices[i].co for i in t.vertices];c=sum(ps,Vector())/3;rx=side*(c.x-.001069)+.001069
  if not(.79<rx<.96 and 1.23<c.z<1.34):continue
  if label=='Thumb':ok=c.y>.139 and rx<.861
  else:ok=abs(c.y-cy)<.022 and rx>.836
  if not ok:continue
  ix=len(points);points.extend(p.copy() for p in ps);ts.append((ix,ix+1,ix+2));uvs.append([Vector((*uvsrc[j].uv,0)) for j in t.loops])
 tree=BVHTree.FromPolygons(points,ts,all_triangles=True)
 def getuv(p):
  near,normal,idx,dist=tree.find_nearest(p);a,b,c=ts[idx];v=barycentric_transform(near,points[a],points[b],points[c],*uvs[idx]);return (v.x,v.y)
 return getuv
# Keep exact original vertex/UV/normal data outside local hand topology changes.
original_signature={tuple(round(c,6) for c in v.co) for v in src.vertices if abs(v.co.x-.001069)<.79}
normal_by_corner={}
for loop in src.loops:
 p=src.vertices[loop.vertex_index].co
 if abs(p.x-.001069)<.79:normal_by_corner[(tuple(round(x,6) for x in p),tuple(round(x,6) for x in uvsrc[loop.index].uv))]=tuple(src.corner_normals[loop.index].vector)
me=src.copy();o.data=me;o.name='SunboundWarden_Original_RebuiltFingers';bm=bmesh.new();bm.from_mesh(me);uvlayer=bm.loops.layers.uv.active;newfaces=[];report=[];recipes=[]
finger_mat=src.materials[0].copy();finger_mat.name='Original_Worn_FingerPaint_Transferred';me.materials.append(finger_mat)
def boundary_groups(edges):
 todo=set(edges);out=[]
 while todo:
  e=todo.pop();vs=[e.verts[0],e.verts[1]];start=vs[0];cur=vs[-1]
  while cur!=start:
   nxt=[x for x in cur.link_edges if x in todo]
   if not nxt:raise RuntimeError('Open digit root; cannot bridge safely')
   ee=nxt[0];todo.remove(ee);cur=ee.other_vert(cur);vs.append(cur)
  out.append(vs[:-1])
 return out
root_records=[]
for side in [1,-1]:
 x=.855 if side==1 else -.852862;ret=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0000001,plane_co=(x,0,0),plane_no=(side,0,0),clear_outer=True,clear_inner=False);edges=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary]
 # Add real cross-sectional vertices at the source interface; UVs on cut body faces interpolate.
 bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=False)
 loops=boundary_groups([e for e in bm.edges if e.is_boundary and all(abs(v.co.x-x)<.000001 for v in e.verts)]);loops.sort(key=lambda ids:sum(v.co.y for v in ids)/len(ids))
 assert len(loops)==4
 # Collect live roots after all BMesh cutting operations.
for side in [1,-1]:
 faces=[f for f in bm.faces if any((v.co.x>.78 if side==1 else v.co.x<-.777862) and 1.23<v.co.z<1.34 for v in f.verts)];geom=set(faces)
 for f in faces:geom.update(f.verts);geom.update(f.edges)
 ret=bmesh.ops.bisect_plane(bm,geom=list(geom),dist=.0000001,plane_co=(0,.150,0),plane_no=(0,1,0),clear_outer=True,clear_inner=False);edges=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary];bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=False)
 loops=boundary_groups([e for e in bm.edges if e.is_boundary and all(abs(v.co.y-.150)<.000001 and (v.co.x>.79 if side==1 else v.co.x<-.787862) for v in e.verts)]);assert len(loops)==1
for side in [1,-1]:
 x=.855 if side==1 else -.852862
 loops=boundary_groups([e for e in bm.edges if e.is_boundary and all(abs(v.co.x-x)<.000001 for v in e.verts)])
 loops.sort(key=lambda vs:sum(v.co.y for v in vs)/len(vs));assert len(loops)==4
 for label,root in zip(['Little','Ring','Middle','Index'],loops):root_records.append((side,label,root))
 loops=boundary_groups([e for e in bm.edges if e.is_boundary and all(abs(v.co.y-.150)<.000001 and (v.co.x>.79 if side==1 else v.co.x<-.787862) for v in e.verts)]);assert len(loops)==1
 root_records.append((side,'Thumb',loops[0]))
params={'Little':(.078,.030,1.269,.0080,.0080),'Ring':(.092,.058,1.284,.0097,.0090),'Middle':(.099,.095,1.287,.0104,.0093),'Index':(.094,.131,1.286,.0100,.0090)}
# Full loop profile: shaft, actual joint rings, distal section, rounded blunt cap.
profile=[(.05,1.08),(.11,1.03),(.26,.96),(.32,1.05),(.38,.96),(.58,.90),(.65,.96),(.71,.89),(.86,.82),(.92,.81),(.955,.76),(.978,.62),(.991,.40),(1.0,.10)]
for side,label,root in root_records:
 c=sum((v.co for v in root),Vector())/len(root);n=len(root);isthumb=label=='Thumb';getter=projector(side,label,c.y)
 if isthumb:
  root.sort(key=lambda v:math.atan2(v.co.z-c.z,side*(v.co.x-c.x)));axis=Vector((side*.63,.76,-.15)).normalized();u=Vector((side*.77,-.64,0)).normalized();baseangle=math.atan2(root[0].co.z-c.z,side*(root[0].co.x-c.x));length=.065;ry=.012;rz=.010
 else:
  root.sort(key=lambda v:math.atan2(v.co.z-c.z,v.co.y-c.y));baseangle=math.atan2(root[0].co.z-c.z,root[0].co.y-c.y);length,end_y,end_z,ry,rz=params[label];axis=Vector((side,0,0));u=Vector((0,1,0))
 root_paint_coords=[v.co.copy() for v in root]
 # Round the cut boundary itself so narrow new shafts do not meet angular source flaps.
 for k,v in enumerate(root):
  aa=baseangle+k*math.tau/n;cc=math.copysign(abs(math.cos(aa))**.64,math.cos(aa));ss=math.copysign(abs(math.sin(aa))**.60,math.sin(aa))
  rootu=Vector((side,0,0)) if isthumb else Vector((0,1,0));target=c+rootu*(ry*1.2*cc)+Vector((0,0,rz*1.18*ss))
  if isthumb:target.y=v.co.y
  else:target.x=v.co.x
  v.co=v.co.lerp(target,.52)
 prev=root;uvtargets={};params_uv={v:(0,k/n) for k,v in enumerate(root)}
 col={'Thumb':0,'Index':1,'Middle':2,'Ring':3,'Little':4}[label];row=0 if side==1 else 1
 recipes.append({'root_paint':root_paint_coords,'side':side,'label':label,'root':c.copy(),'ry':ry,'rz':rz,'angle':baseangle,'getter':getter,'col':col,'row':row,'end_y':None if isthumb else end_y,'end_z':None if isthumb else end_z})
 def tile_uv(t,phi):return (col*.2+.008+.184*t,row*.5+.015+.470*phi)
 for t,factor in profile:
  if isthumb:
   center=c+axis*length*t;center.z-=.002*math.sin(t*math.pi);up=Vector((0,0,1));old_center=c+Vector((side*.020,.015,-.003))*t
  else:
   s=t*t*(3-2*t);center=Vector((c.x+side*length*t,c.y+(end_y-c.y)*s,c.z+(end_z-c.z)*s));up=Vector((0,0,1));oldmax={'Little':.910604,'Ring':.940239,'Middle':.946688,'Index':.923103}[label];old_center=Vector((c.x+side*(oldmax-.855)*t,c.y+(end_y-c.y)*t,c.z+(end_z-c.z)*t))
  rr=ry*factor;zz=rz*factor;cur=[]
  for k in range(n):
   a=baseangle+k*math.tau/n;ca=math.copysign(abs(math.cos(a))**.62,math.cos(a));sa=math.copysign(abs(math.sin(a))**(.46 if math.sin(a)>0 else .72),math.sin(a));p=center+u*rr*ca+up*zz*sa
   # First ring matches the irregular original root smoothly before taking the new profile.
   if t<.18:p=root[k].co.lerp(p,t/.18)
   v=bm.verts.new(p);cur.append(v);params_uv[v]=(t,k/n)
   sample=old_center+u*ry*factor*ca+up*rz*factor*sa;uvtargets[v]=getter(sample)
  for k in range(n):
   f=bm.faces.new((prev[k],prev[(k+1)%n],cur[(k+1)%n],cur[k]));f.smooth=True;f.material_index=1;newfaces.append(f)
   for loop in f.loops:
    tt,phi=params_uv[loop.vert]
    if k==n-1 and phi==0:phi=1
    loop[uvlayer].uv=tile_uv(tt,phi)
  prev=cur
 f=bm.faces.new(tuple(prev));f.smooth=True;f.material_index=1;newfaces.append(f)
 for loop in f.loops:
  tt,phi=params_uv[loop.vert];loop[uvlayer].uv=tile_uv(.998+.0004*math.cos(phi*math.tau),phi)
 report.append({'side':side,'digit':label,'length_from_cut_root_m':length,'ring_vertices':n,'longitudinal_rings':len(profile),'cap_width_fraction_of_start':.60})
# Smooth only old thumb-web and little-finger attachment remnants; exact root boundary is retained.
newverts={v for f in newfaces for v in f.verts}
soft=[v for v in bm.verts if v not in newverts and .813<abs(v.co.x-.001069)<.854 and ((.130<v.co.y<.150) or (.016<v.co.y<.052)) and 1.245<v.co.z<1.315]
for _ in range(5):bmesh.ops.smooth_vert(bm,verts=soft,factor=.36,use_axis_x=True,use_axis_y=True,use_axis_z=True)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
# Restore source corner normals on untouched armor/body; allow true new normals on digits.
normal_list=[];newuv=me.uv_layers.active.data
for loop in me.loops:
 p=me.vertices[loop.vertex_index].co;key=(tuple(round(x,6) for x in p),tuple(round(x,6) for x in newuv[loop.index].uv));normal_list.append(normal_by_corner.get(key,tuple(me.corner_normals[loop.index].vector)))
me.normals_split_custom_set(normal_list)
actual={tuple(round(c,6) for c in v.co) for v in me.vertices if abs(v.co.x-.001069)<.79};assert actual==original_signature
# A local painted atlas samples original surfaces per pixel, avoiding interpolation across source UV islands.
W,H=1024,512
uv_pixels=np.zeros((H,W,2),dtype=np.float32)
for recipe in recipes:
 side=recipe['side'];label=recipe['label'];c=recipe['root'];ry=recipe['ry'];rz=recipe['rz'];baseangle=recipe['angle'];getter=recipe['getter'];col=recipe['col'];row=recipe['row'];thumb=label=='Thumb';u=Vector((side*.77,-.64,0)).normalized() if thumb else Vector((0,1,0));up=Vector((0,0,1))
 for yy in range(row*256,(row+1)*256):
  phi=max(0,min(1,((yy+.5)/H-row*.5-.015)/.470));a=baseangle+phi*math.tau;ca=math.copysign(abs(math.cos(a))**.62,math.cos(a));sa=math.copysign(abs(math.sin(a))**.62,math.sin(a))
  for xx in range(int(col*W/5),int((col+1)*W/5)):
   t=max(0,min(1,((xx+.5)/W-col*.2-.008)/.184));fac=float(np.interp(t,[0]+[z[0] for z in profile],[1]+[z[1] for z in profile]))
   if thumb:oc=c+Vector((side*.020,.015,-.003))*t
   else:
    oldmax={'Little':.910604,'Ring':.940239,'Middle':.946688,'Index':.923103}[label];oc=Vector((c.x+side*(oldmax-.855)*t,c.y+(recipe['end_y']-c.y)*t,c.z+(recipe['end_z']-c.z)*t))
   sample=oc+u*ry*fac*ca+up*rz*fac*sa
   if t<.20:
    rp=recipe['root_paint'];fraction=phi*len(rp);i=int(fraction)%len(rp);rootpoint=rp[i].lerp(rp[(i+1)%len(rp)],fraction-int(fraction));w=t/.20;w=w*w*(3-2*w);sample=rootpoint.lerp(sample,w)
   uv_pixels[yy,xx]=getter(sample)
for node in finger_mat.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image:
  source=node.image;sw,sh=source.size;buf=np.empty(sw*sh*4,dtype=np.float32);source.pixels.foreach_get(buf);buf=buf.reshape(sh,sw,4);ux=np.mod(uv_pixels[:,:,0],1)*(sw-1);vy=np.mod(uv_pixels[:,:,1],1)*(sh-1);x0=np.floor(ux).astype(np.int32);y0=np.floor(vy).astype(np.int32);x1=np.minimum(x0+1,sw-1);y1=np.minimum(y0+1,sh-1);fx=(ux-x0)[:,:,None];fy=(vy-y0)[:,:,None];out=buf[y0,x0]*(1-fx)*(1-fy)+buf[y0,x1]*fx*(1-fy)+buf[y1,x0]*(1-fx)*fy+buf[y1,x1]*fx*fy
  im=bpy.data.images.new('FingerPaint_'+source.name,width=W,height=H,alpha=True);im.colorspace_settings.name=source.colorspace_settings.name;im.pixels.foreach_set(out.astype(np.float32).ravel());im.filepath_raw=str(OUT/(im.name+'.png'));im.file_format='PNG';im.save();im.pack();node.image=im
  if 'diffuse' in source.name:finger_mat.node_tree.nodes.active=node
for node in finger_mat.node_tree.nodes:
 if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.6

# Source maps remain the appearance authority; no new generic glove materials.
for mat in me.materials:
 for node in mat.node_tree.nodes:
  if node.type=='TEX_IMAGE' and node.image and node.image.has_data and not node.image.packed_file:node.image.pack()
qa={'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'original_vertices':len(src.vertices),'reworked_vertices':len(me.vertices),'original_faces':len(src.polygons),'reworked_faces':len(me.polygons),'body_positions_preserved':actual==original_signature,'source_materials_retained':[m.name for m in me.materials],'digits':report,'rigged':False};(OUT/'geometry_qa.json').write_text(json.dumps(qa,indent=2))
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world=sc.world or bpy.data.worlds.new('ReviewWorld');sc.world.color=(.1,.1,.1);sc.display.shading.color_type='TEXTURE';sc.display.shading.light='STUDIO';sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='ReviewCamera';cam.data.type='ORTHO';sc.camera=cam;centerbody=Vector((.001,0,.847));cam.data.ortho_scale=2.17;cam.location=centerbody+Vector((0,-4,0));cam.rotation_euler=(centerbody-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='TEXTURE';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_RebuiltFingers_v004.blend'))
sc.render.filepath=str(OUT/'Rework_Full.png');bpy.ops.render.render(write_still=True)
center=Vector((.866,.098,1.284));cam.data.ortho_scale=.340
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandOblique',(1,-1,2)),('HandFront',(0,-2,.4))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
# Original image under identical camera settings.
o.data=src;cam.location=center+Vector((0,0,2));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'Original_HandTop.png');bpy.ops.render.render(write_still=True)
print('DIGITS_REBUILT',qa['reworked_vertices'],qa['reworked_faces'])

