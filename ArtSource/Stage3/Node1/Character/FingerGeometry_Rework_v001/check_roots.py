import bpy,bmesh,pathlib,json
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/FingerGeometry_Rework_v001');bpy.ops.wm.open_mainfile(filepath='C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend');m=bpy.data.objects['model'].data;bm=bmesh.new();bm.from_mesh(m)
def loops(edges):
 todo=set(edges);out=[]
 while todo:
  e=todo.pop();vs=[e.verts[0],e.verts[1]];start=vs[0];cur=vs[-1]
  while cur!=start:
   es=[x for x in cur.link_edges if x in todo]
   if not es:break
   ee=es[0];todo.remove(ee);cur=ee.other_vert(cur);vs.append(cur)
  out.append({'closed':vs[-1]==start,'n':len(vs)-1,'max_y':max(v.co.y for v in vs),'mean':[sum(v.co[k] for v in vs[:-1])/(len(vs)-1) for k in range(3)]})
 return out
for side in [1,-1]:
 x=.855 if side==1 else -.852862;ret=bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0000001,plane_co=(x,0,0),plane_no=(side,0,0),clear_outer=True,clear_inner=False);edges=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary];print('FOUR',side,loops(edges))
for side in [1,-1]:
 faces=[f for f in bm.faces if any((v.co.x>.78 if side==1 else v.co.x<-.777862) and 1.23<v.co.z<1.34 for v in f.verts)];geom=set(faces)
 for f in faces:geom.update(f.verts);geom.update(f.edges)
 ret=bmesh.ops.bisect_plane(bm,geom=list(geom),dist=.0000001,plane_co=(0,.150,0),plane_no=(0,1,0),clear_outer=True,clear_inner=False);edges=[e for e in ret['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and e.is_boundary];print('THUMB',side,loops(edges))


