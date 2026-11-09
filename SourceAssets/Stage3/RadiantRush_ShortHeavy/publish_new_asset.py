from pathlib import Path
import json,re,uuid,hashlib
R=Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE');O=R/'SourceAssets/Stage3/RadiantRush_ShortHeavy';A=R/'Assets/Stage 3/Enemies/STAGE 3 ENEMY NO. 1/Animations'
name='SunboundWarden_RadiantRush_ShortHeavy';src=O/(name+'.fbx');dest=A/src.name
v=json.loads((O/'export_validation.json').read_text());c=json.loads((O/'continuous_motion_validation.json').read_text())
assert not v['changed_original_files'] and v['names_and_parents_match']
assert c['nla_to_fbx_max_bone_position_error_m'][0]<.0001
assert not dest.exists() and not Path(str(dest)+'.meta').exists(),'Never replace an existing Unity asset'
# New importer only: use current Rush import conventions, with a fresh GUID,
# auto-map the unchanged canonical skeleton, and author no Animation Events.
meta=(A/'SunboundWarden_RadiantRush.fbx.meta').read_text()
meta=re.sub(r'^guid: .*$', 'guid: '+uuid.uuid4().hex,meta,flags=re.M)
meta=meta.replace('materialImportMode: 2','materialImportMode: 0').replace('resampleCurves: 1','resampleCurves: 0').replace('animationCompression: 3','animationCompression: 0')
meta=meta.replace('name: Radiant Rush v2','name: '+name).replace('takeName: Scene','takeName: '+name).replace('firstFrame: 2','firstFrame: 1').replace('lastFrame: 85','lastFrame: 75')
meta=re.sub(r'^      internalID: .*\n','',meta,flags=re.M)
for prop in ['loopBlendOrientation','loopBlendPositionY','loopBlendPositionXZ','keepOriginalOrientation','keepOriginalPositionXZ']:
 meta=meta.replace(prop+': 0',prop+': 1')
human='''  humanDescription:
    serializedVersion: 3
    human: []
    skeleton: []
    armTwist: 0.5
    foreArmTwist: 0.5
    upperLegTwist: 0.5
    lowerLegTwist: 0.5
    armStretch: 0.05
    legStretch: 0.05
    feetSpacing: 0
    globalScale: 1
    rootMotionBoneName:
    hasTranslationDoF: 0
    hasExtraRoot: 1
    skeletonHasParents: 1
'''
human=human.replace('upperLegTwist','upperLegTwist').replace('lowerLegTwist','legTwist')
meta=re.sub(r'  humanDescription:\n.*?(?=  lastHumanDescriptionAvatarSource:)',human,meta,flags=re.S)
with Path(str(dest)+'.meta').open('x',encoding='utf-8',newline='\n') as f:f.write(meta)
with dest.open('xb') as f:f.write(src.read_bytes())
manifest=json.loads((O/'preservation_manifest.json').read_text());changed=[p for p,h in manifest.items() if hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
report={'export_path':str(dest),'sha256':hashlib.sha256(dest.read_bytes()).hexdigest(),'matches_validated_export':dest.read_bytes()==src.read_bytes(),'preserved_file_count':len(manifest),'changed_original_files':changed,'new_importer_settings_authored':{'rig':'Humanoid / Create From This Model','loop_time':False,'root_rotation_and_position_baked_into_pose':True,'compression':'Off','resample_curves':False,'events':[]},'unity_import_runtime_validation':'Not performed'}
(O/'delivery_validation.json').write_text(json.dumps(report,indent=2));assert not changed;print(json.dumps(report,indent=2))
