import bpy,pathlib,json,numpy as np
OUT=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/ReflectiveBody_Materials_v001');P=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001/SunboundWarden_TargetedRework_FINAL.blend');bpy.ops.wm.open_mainfile(filepath=str(P));result={'materials':[]}
for name in ['model','Warden_Gauntlet_AntiqueGold','Warden_Gauntlet_EdgeGold','Warden_Glove_DeepPurple']:
 m=bpy.data.materials.get(name)
 if not m:continue
 row={'name':name,'nodes':[]}
 for n in m.node_tree.nodes:
  r={'name':n.name,'type':n.type}
  if n.type=='BSDF_PRINCIPLED':r['inputs']={x.name:{'value':list(x.default_value) if hasattr(x.default_value,'__len__') else x.default_value,'links':[(a.from_node.name,a.from_socket.name) for a in x.links]} for x in n.inputs if x.name in ['Base Color','Metallic','Roughness','Specular IOR Level','IOR','Normal','Coat Weight','Coat Roughness']}
  if n.type=='TEX_IMAGE' and n.image:
   im=n.image;r['image']=im.name;r['color_space']=im.colorspace_settings.name
   try:
    sz=im.size;buf=np.empty(sz[0]*sz[1]*4,dtype=np.float32);im.pixels.foreach_get(buf);v=buf.reshape(sz[1],sz[0],4)[::8,::8,:3];r['stats']={'min':v.min(axis=(0,1)).tolist(),'max':v.max(axis=(0,1)).tolist(),'mean':v.mean(axis=(0,1)).tolist(),'quantiles_r':np.quantile(v[:,:,0],[0,.1,.25,.5,.75,.9,1]).tolist()}
   except Exception as e:r['error']=str(e)
  row['nodes'].append(r)
 result['materials'].append(row)
(OUT/'material_inspection.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
