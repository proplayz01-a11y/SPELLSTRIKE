import bpy,bmesh,math,pathlib,json,time,random
import numpy as np
from mathutils import Vector
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Characters/StoneGolem/VolumetricStone_v001');SOURCE=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Characters/StoneGolem/Mixamo_Package_v001/StoneGolem_Mixamo_Copy.blend');bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene;source=bpy.data.objects['StoneGolem_Mixamo'];original_mesh=source.data.copy();source.name='REFERENCE_Source_Golem';source.hide_render=True;source.hide_set(True)
ref=bpy.data.collections.new('REFERENCE_Original');sc.collection.children.link(ref)
for c in list(source.users_collection):c.objects.unlink(source)
ref.objects.link(source);ref.hide_render=True
rocks=bpy.data.collections.new('GOLEM_Actual_Stone_Pieces');sc.collection.children.link(rocks);helpers=bpy.data.collections.new('AUTHORING_Construction');sc.collection.children.link(helpers)
def mat(name,col,rough=.82):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True;nt=m.node_tree;p=nt.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=0
 tex=nt.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=48;tex.inputs['Detail'].default_value=3;r=nt.nodes.new('ShaderNodeValToRGB');r.color_ramp.elements[0].color=(*(x*.70 for x in col),1);r.color_ramp.elements[1].color=(*(min(1,x*1.22) for x in col),1);nt.links.new(tex.outputs['Fac'],r.inputs[0]);nt.links.new(r.outputs[0],p.inputs['Base Color']);micro=nt.nodes.new('ShaderNodeTexNoise');micro.inputs['Scale'].default_value=175;micro.inputs['Detail'].default_value=2;b=nt.nodes.new('ShaderNodeBump');b.inputs['Distance'].default_value=.0008;b.inputs['Strength'].default_value=.22;nt.links.new(micro.outputs['Fac'],b.inputs['Height']);nt.links.new(b.outputs['Normal'],p.inputs['Normal']);return m
palette=[mat('Stone_Granite_%02d'%i,(.105*f,.115*f,.098*f)) for i,f in enumerate([.73,.84,.94,1.0,1.06,1.16,1.27])];dark=mat('Stone_Deep_Crevices',(.025,.030,.025),.94)
core=source.copy();core.data=original_mesh.copy();core.name='Golem_Dark_Joint_Core';rocks.objects.link(core);core.hide_render=False;core.hide_set(False);core.data.materials.clear();core.data.materials.append(dark)
# Slight recess only on the helper core. All visible macro structure comes from real volumetric cells.
for v in core.data.vertices:
 p=v.co.copy();p.x*=.975;p.y*=.96;p.z=(p.z-1)*.985+1;v.co=p
master=source.copy();master.data=original_mesh.copy();master.name='AUTHOR_Watertight_Fracture_Base';helpers.objects.link(master);master.hide_set(False);master.hide_render=False;bpy.ops.object.select_all(action='DESELECT');master.select_set(True);bpy.context.view_layer.objects.active=master
rem=master.modifiers.new('Watertight stone volume','REMESH');rem.mode='VOXEL';rem.voxel_size=.013;rem.use_smooth_shade=False;bpy.ops.object.modifier_apply(modifier=rem.name)
dec=master.modifiers.new('Construction simplification','DECIMATE');dec.ratio=.36;bpy.ops.object.modifier_apply(modifier=dec.name);base=master.data.copy();master.hide_render=True;master.hide_set(True);helpers.hide_render=True
# Surface-distributed cell seeds, with a weighted head cell to keep the recognizable face.
pts=np.array([list(v.co) for v in original_mesh.vertices]);norms=np.array([list(v.normal) for v in original_mesh.vertices]);eligible=(~((np.abs(pts[:,0])<.24)&(pts[:,2]>1.52)));candidates=pts[eligible];candidate_normals=norms[eligible];rng=np.random.default_rng(76);order=rng.permutation(len(candidates));candidates=candidates[order];candidate_normals=candidate_normals[order]
seeds=[np.array([0,-.12,1.65])];snorm=[np.array([0,-1,0])];d=np.sum((candidates-seeds[0])**2,axis=1)
for i in range(135):
 idx=int(np.argmax(d));seeds.append(candidates[idx].copy());snorm.append(candidate_normals[idx].copy());new=np.sum((candidates-candidates[idx])**2,axis=1);d=np.minimum(d,new)
seeds=np.array(seeds);weights=np.zeros(len(seeds));weights[0]=.035;random.seed(76);stats=[];start=time.time()
for i,seed in enumerate(seeds):
 bm=bmesh.new();bm.from_mesh(original_mesh if i==0 else base)
 # Half-space cuts create actual filled stone cells, not raised texture lines.
 neighbors=list(np.argsort(np.sum((seeds-seed)**2,axis=1))[1:25])
 if i!=0 and 0 not in neighbors:neighbors.append(0)
 for j in neighbors:
  n=seeds[j]-seed;length=np.linalg.norm(n)
  if length<1e-6:continue
  midpoint=(seeds[j]+seed)*.5+n*((weights[i]-weights[j])/(2*length*length));ret=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=midpoint,plane_no=n,clear_outer=True,clear_inner=False)
  es=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary]
  if es:
   fill=bmesh.ops.holes_fill(bm,edges=es,sides=0)
   for f in fill.get('faces',[]):f.material_index=1;f.smooth=False
  if not bm.faces:break
 if len(bm.faces)<4:bm.free();continue
 center=sum((v.co for v in bm.verts),Vector())/len(bm.verts)
 if i!=0:
  # A few small planar corner chips add real uneven facets to the cell edges.
  for _ in range(2):
   far=max(bm.verts,key=lambda v:(v.co-center).length*random.uniform(.8,1.2));n=(far.co-center).normalized();co=far.co-n*random.uniform(.002,.007);ret=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.000001,plane_co=co,plane_no=n,clear_outer=True)
   es=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary]
   if es:
    for f in bmesh.ops.holes_fill(bm,edges=es,sides=0).get('faces',[]):f.material_index=0
  shrink=random.uniform(.925,.960);outward=Vector(snorm[i]).normalized()*random.uniform(.006,.014)
  for v in bm.verts:v.co=center+(v.co-center)*shrink+outward
 else:
  for v in bm.verts:v.co=center+(v.co-center)*.995
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));me=bpy.data.meshes.new('StoneCell_%03d'%i);bm.to_mesh(me);bm.free();obj=bpy.data.objects.new('Golem_Carved_Head' if i==0 else 'Golem_Rock_%03d'%i,me);rocks.objects.link(obj)
 if i==0:
  headmat=source.data.materials[0].copy();headmat.name='Carved_Stone_Face';headmat.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.82;me.materials.append(headmat);me.materials.append(dark)
 else:me.materials.append(random.choice(palette));me.materials.append(palette[1])
 for f in me.polygons:f.use_smooth=(i==0)
 if i!=0:
  bevel=obj.modifiers.new('Chipped rounded stone edges','BEVEL');bevel.width=.0025;bevel.segments=1;bevel.limit_method='ANGLE';bevel.angle_limit=.42;obj.modifiers.new('Broad stone face normals','WEIGHTED_NORMAL')
 obj['construction']='Closed Voronoi-cut volume with true gaps and corner chips';stats.append({'name':obj.name,'vertices':len(me.vertices),'faces':len(me.polygons)})
 if i%20==0:print('STONE_PROGRESS',i,len(seeds),'seconds',round(time.time()-start,1),flush=True)
(OUT/'stone_geometry.json').write_text(json.dumps({'source':str(SOURCE),'stone_count':len(stats),'pieces':stats},indent=2));cam=sc.camera;cam.location=(0,-6,1);cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.85;sc.render.engine='BLENDER_WORKBENCH';sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.resolution_percentage=100;sc.display.shading.color_type='MATERIAL';sc.display.shading.light='STUDIO';sc.display.shading.show_cavity=True;sc.display.shading.show_shadows=True
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.color_type='MATERIAL';area.spaces.active.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'StoneGolem_ActualStone_v001.blend'))
for name,pos in [('Front',(0,-6,1)),('ThreeQuarter',(3,-5,2.0)),('Back',(0,6,1))]:
 cam.location=pos;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();sc.render.filepath=str(OUT/('Geometry_'+name+'.png'));bpy.ops.render.render(write_still=True)
print('STONE_BUILD_DONE',len(stats),round(time.time()-start,1),flush=True)
