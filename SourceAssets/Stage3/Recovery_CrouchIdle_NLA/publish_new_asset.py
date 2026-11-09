from pathlib import Path
import json,re,uuid,hashlib
R=Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/Recovery_CrouchIdle_NLA';A=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations';name='SunboundWarden_Recovery_CrouchIdle';prior='SunboundWarden_RadiantRush_ShortHeavy'
v=json.loads((O/'export_validation.json').read_text());assert not v['changed_original_files'] and v['names_and_parents_match'] and v['fbx_max_bone_position_error_m']<.0001
dest=A/(name+'.fbx');meta_path=Path(str(dest)+'.meta');assert not dest.exists() and not meta_path.exists()
meta=(A/(prior+'.fbx.meta')).read_text().replace(prior,name)
meta=re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex,meta,flags=re.M)
meta=re.sub(r'      firstFrame: .*','      firstFrame: 1',meta);meta=re.sub(r'      lastFrame: .*','      lastFrame: 91',meta)
assert 'events: []' in meta and 'loopTime: 0' in meta
with meta_path.open('x',encoding='utf-8',newline='\n') as f:f.write(meta)
data=(O/(name+'.fbx')).read_bytes()
with dest.open('xb') as f:f.write(data)
manifest=json.loads((O/'preservation_manifest.json').read_text());changed=[p for p,h in manifest.items() if hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
report={'export_path':str(dest),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'matches_validated_export':dest.read_bytes()==data,'preserved_file_count':len(manifest),'changed_original_files':changed,'unity_import_runtime_validation':'Not performed'}
(O/'delivery_validation.json').write_text(json.dumps(report,indent=2));assert not changed;print(json.dumps(report,indent=2))
