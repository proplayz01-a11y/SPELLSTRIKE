import bpy,pathlib,json,math,hashlib
from mathutils import Vector
from mathutils.kdtree import KDTree
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/OriginalHands_Rework_v001');SOURCE=pathlib.Path('C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend')
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;o=bpy.data.objects['model'];o.name='SunboundWarden_Original_RefinedHands';me=o.data;original=[v.co.copy() for v in me.vertices];original_uv=[tuple(p.uv) for p in me.uv_layers.active.data]
parts=json.loads((OUT/'finger_components.json').read_text())['0.855'];labels=['Little','Ring','Middle','Index'];extend=[.015,.002,.001,.015];mapping={};tip_weights={};groups={}
for label,part,dx in zip(labels,parts,extend):
 ids=part['ids'];mx=part['bounds'][1][0];base=.851;groups[label]=ids
 for i in ids:
  p=original[i].copy();t=max(0,min(1,(p.x-base)/(mx-base)));delta=Vector((dx*t**1.22,0,0))
  if label=='Little':delta.y+=.006*t;delta.z+=.004*t
  mapping[i]=delta;tip_weights[i]=max(0,min(1,(t-.79)/.21))
# Thumb follows its own diagonal, retaining opposition instead of a parallel fifth finger.
root=Vector((.814,.137,1.28));axis=Vector((.78,.625,0)).normalized()
thumb=[]
for i,p in enumerate(original):
 if p.x>.814 and p.y>.137 and p.x<.857 and 1.24<p.z<1.31:
  t=max(0,min(1,(p-root).dot(axis)/.040));fade=max(0,min(1,(p.y-.137)/.010));delta=axis*(.016*t**1.35*fade);delta.z-=.001*t;mapping[i]=delta;thumb.append(i);tip_weights[i]=max(0,min(1,(t-.74)/.26))
groups['Thumb']=thumb
# Mirror the edit onto the corresponding existing vertices, leaving source topology and UVs intact.
center=.001069;rightids=[i for i,p in enumerate(original) if p.x>.79 and p.z>1.2];kd=KDTree(len(rightids))
for i in rightids:kd.insert(original[i],i)
kd.balance();mapped_left=[];maxdist=0
for i,p in enumerate(original):
 if p.x<-.78 and p.z>1.2:
  target=Vector((2*center-p.x,p.y,p.z));pos,idx,dist=kd.find(target)
  if idx in mapping and dist<.00002:
   d=mapping[idx];mapping[i]=Vector((-d.x,d.y,d.z));tip_weights[i]=tip_weights[idx];mapped_left.append(i);maxdist=max(maxdist,dist)
for i,d in mapping.items():me.vertices[i].co=original[i]+d
me.update()
# Adjustable local corner softening, confined to the original distal finger geometry.
vg=o.vertex_groups.new(name='Rework_Fingertip_Softening')
for i,w in tip_weights.items():
 if w>0:vg.add([i],w,'REPLACE')
b=o.modifiers.new('Rework - soften original fingertip corners','BEVEL');b.limit_method='VGROUP';b.vertex_group=vg.name;b.affect='VERTICES';b.width=.0017;b.segments=3;b.harden_normals=True;b.use_clamp_overlap=True
# Add tuned digit selections for later artist adjustment.
for label,ids in groups.items():
 g=o.vertex_groups.new(name='Rework_'+label+'_R');g.add(ids,1,'REPLACE')
# Pack only actual loaded images; unused missing source references are left untouched.
used_images=[]
for m in me.materials:
 if m and m.use_nodes:
  for node in m.node_tree.nodes:
   if node.type=='TEX_IMAGE' and node.image:
    im=node.image;used_images.append(im.name)
    if not im.has_data:
     try:_=im.pixels[0]
     except Exception:pass
    if im.has_data and not im.packed_file:im.pack()
qa={'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'source_vertices':len(original),'rework_vertices':len(me.vertices),'source_faces':len(me.polygons),'changed_vertices':len(mapping),'mirrored_vertex_error':maxdist,'uvs_exactly_unchanged':original_uv==[tuple(p.uv) for p in me.uv_layers.active.data],'outside_hands_unchanged':all(v.co==p for v,p in zip(me.vertices,original) if abs(p.x-center)<.79),'finger_extension_m':dict(zip(labels,extend)),'thumb_diagonal_extension_m':.016,'used_images':used_images,'rigged':any(x.type=='ARMATURE' for x in sc.objects)}
assert qa['uvs_exactly_unchanged'] and qa['outside_hands_unchanged']
(OUT/'geometry_qa.json').write_text(json.dumps(qa,indent=2))
notes=bpy.data.texts.new('README_Original_Hands_Rework');notes.write('Faithful original-hand refinement, 2026-10-05. Existing mesh, palm/cuffs, painted materials and exact original UV coordinates retained. Edits: modest digit-specific extensions, straighter little-finger silhouette, diagonal thumb extension, tunable local tip bevel. Original file is unchanged. No generic replacement gloves or plates. Unrigged; animation/grip and Unity validation pending.\n')
# Neutral review studio uses the original material texture appearance.
sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.world=sc.world or bpy.data.worlds.new('ReviewWorld');sc.world.color=(.10,.10,.10);sc.display.shading.light='STUDIO';sc.display.shading.color_type='TEXTURE';sc.display.shading.show_shadows=True;sc.display.shading.show_cavity=True
bpy.ops.object.camera_add();cam=bpy.context.object;cam.name='ReviewCamera';cam.data.type='ORTHO';sc.camera=cam
centerbody=Vector((.001,0,.847));cam.data.ortho_scale=2.17;cam.location=centerbody+Vector((0,-4,0));cam.rotation_euler=(centerbody-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.context.view_layer.objects.active=o;o.select_set(True)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='TEXTURE';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'SunboundWarden_OriginalHands_Reworked_v001.blend'))
sc.render.filepath=str(OUT/'Rework_Full_Front.png');bpy.ops.render.render(write_still=True)
center=Vector((.866,.088,1.286));cam.data.ortho_scale=.315
for name,d in [('HandTop',(0,0,2)),('HandPalm',(0,0,-2)),('HandOblique',(1,-1,2)),('HandFront',(0,-2,.4))]:
 cam.location=center+Vector(d);cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('Rework_'+name+'.png'));bpy.ops.render.render(write_still=True)
# Same camera and lighting: original before image.
for i,p in enumerate(original):me.vertices[i].co=p
b.show_render=False;me.update();cam.location=center+Vector((0,0,2));cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/'Original_HandTop.png');bpy.ops.render.render(write_still=True)
print('REWORK_PASS',qa)
