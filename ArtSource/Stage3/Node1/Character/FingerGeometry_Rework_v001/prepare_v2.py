from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001/rebuild_fingers.py');s=p.read_text()
s=s.replace('from mathutils.geometry import barycentric_transform','from mathutils.geometry import barycentric_transform\nimport numpy as np')
s=s.replace("newfaces=[];report=[]","newfaces=[];report=[];recipes=[]\nfinger_mat=src.materials[0].copy();finger_mat.name='Original_Worn_FingerPaint_Transferred';me.materials.append(finger_mat)")
s=s.replace("(.955,.76),(.98,.68),(1.0,.60)","(.955,.76),(.978,.62),(.991,.40),(1.0,.10)")
s=s.replace("prev=root;uvtargets={}","prev=root;uvtargets={};params_uv={v:(0,k/n) for k,v in enumerate(root)}\n col={'Thumb':0,'Index':1,'Middle':2,'Ring':3,'Little':4}[label];row=0 if side==1 else 1\n recipes.append({'side':side,'label':label,'root':c.copy(),'ry':ry,'rz':rz,'angle':baseangle,'getter':getter,'col':col,'row':row,'end_y':None if isthumb else end_y,'end_z':None if isthumb else end_z})\n def tile_uv(t,phi):return (col*.2+.008+.184*t,row*.5+.015+.470*phi)")
s=s.replace("**.62,math.sin(a)","**(.46 if math.sin(a)>0 else .72),math.sin(a)")
s=s.replace("if t<.11:p=root[k].co.lerp(p,t/.11)","if t<.18:p=root[k].co.lerp(p,t/.18)")
s=s.replace("v=bm.verts.new(p);cur.append(v)","v=bm.verts.new(p);cur.append(v);params_uv[v]=(t,k/n)")
s=s.replace("f.smooth=True;newfaces.append(f)","f.smooth=True;f.material_index=1;newfaces.append(f)")
s=s.replace("if loop.vert in uvtargets:loop[uvlayer].uv=uvtargets[loop.vert]\n    else:loop[uvlayer].uv=getter(loop.vert.co)","tt,phi=params_uv[loop.vert]\n    if k==n-1 and phi==0:phi=1\n    loop[uvlayer].uv=tile_uv(tt,phi)")
s=s.replace("f.smooth=False;newfaces.append(f)","f.smooth=True;f.material_index=1;newfaces.append(f)")
s=s.replace("for loop in f.loops:loop[uvlayer].uv=uvtargets[loop.vert]", "for loop in f.loops:\n  tt,phi=params_uv[loop.vert];loop[uvlayer].uv=tile_uv(.998+.0004*math.cos(phi*math.tau),phi)")
s=s.replace("bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));", "# Smooth only old thumb-web and little-finger attachment remnants; exact root boundary is retained.\nnewverts={v for f in newfaces for v in f.verts}\nsoft=[v for v in bm.verts if v not in newverts and .813<abs(v.co.x-.001069)<.854 and ((.130<v.co.y<.150) or (.016<v.co.y<.052)) and 1.245<v.co.z<1.315]\nfor _ in range(3):bmesh.ops.smooth_vert(bm,verts=soft,factor=.32,use_axis_x=True,use_axis_y=True,use_axis_z=True)\nbmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));")
marker="# Source maps remain the appearance authority; no new generic glove materials."
atlas=r'''# A local painted atlas samples original surfaces per pixel, avoiding interpolation across source UV islands.
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
   sample=oc+u*ry*fac*ca+up*rz*fac*sa;uv_pixels[yy,xx]=getter(sample)
for node in finger_mat.node_tree.nodes:
 if node.type=='TEX_IMAGE' and node.image:
  source=node.image;sw,sh=source.size;buf=np.empty(sw*sh*4,dtype=np.float32);source.pixels.foreach_get(buf);buf=buf.reshape(sh,sw,4);ux=np.mod(uv_pixels[:,:,0],1)*(sw-1);vy=np.mod(uv_pixels[:,:,1],1)*(sh-1);x0=np.floor(ux).astype(np.int32);y0=np.floor(vy).astype(np.int32);x1=np.minimum(x0+1,sw-1);y1=np.minimum(y0+1,sh-1);fx=(ux-x0)[:,:,None];fy=(vy-y0)[:,:,None];out=buf[y0,x0]*(1-fx)*(1-fy)+buf[y0,x1]*fx*(1-fy)+buf[y1,x0]*(1-fx)*fy+buf[y1,x1]*fx*fy
  im=bpy.data.images.new('FingerPaint_'+source.name,width=W,height=H,alpha=True);im.colorspace_settings.name=source.colorspace_settings.name;im.pixels.foreach_set(out.astype(np.float32).ravel());im.filepath_raw=str(OUT/(im.name+'.png'));im.file_format='PNG';im.save();im.pack();node.image=im
  if 'diffuse' in source.name:finger_mat.node_tree.nodes.active=node
for node in finger_mat.node_tree.nodes:
 if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.6
'''
s=s.replace(marker,atlas+'\n'+marker)
s=s.replace("'SunboundWarden_RebuiltFingers_v001.blend'","'SunboundWarden_RebuiltFingers_v002.blend'")
p2=p.with_name('rebuild_fingers_v2.py');p2.write_text(s)
