import bpy,pathlib,json
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/OriginalHands_Rework_v001');bpy.ops.wm.open_mainfile(filepath='C:/Users/David/OneDrive/Desktop/STAGE 3 CHARACTER NO. 1.blend');o=bpy.data.objects['model'];me=o.data
adj=[set() for v in me.vertices]
for e in me.edges:a,b=e.vertices;adj[a].add(b);adj[b].add(a)
result={}
for cut in [.83,.84,.845,.85,.855,.86,.865,.87]:
 eligible={v.index for v in me.vertices if v.co.x>cut and 1.22<v.co.z<1.36};parts=[]
 while eligible:
  start=eligible.pop();seen={start};todo=[start]
  while todo:
   a=todo.pop()
   for b in adj[a]&eligible:eligible.remove(b);seen.add(b);todo.append(b)
  coords=[me.vertices[i].co for i in seen];parts.append({'ids':sorted(seen),'count':len(seen),'bounds':[[min(p[k] for p in coords) for k in range(3)],[max(p[k] for p in coords) for k in range(3)]],'mean_y':sum(p.y for p in coords)/len(coords)})
 parts.sort(key=lambda x:x['mean_y']);result[str(cut)]=parts;print(cut,[(p['count'],[round(v,4) for v in p['bounds'][1]],round(p['mean_y'],4)) for p in parts])
(OUT/'finger_components.json').write_text(json.dumps(result,indent=2))
(OUT/'source_geometry.json').write_text(json.dumps({'vertices':[list(v.co) for v in me.vertices],'edges':[list(e.vertices) for e in me.edges],'polygons':[list(p.vertices) for p in me.polygons]},indent=2))
