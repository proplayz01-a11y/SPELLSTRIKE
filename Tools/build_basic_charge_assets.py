"""Author only the Basic charge graph/prefab and append its canonical-player binding.

Uses the installed Shader Graph template serialization. No Unity scene writes.
Run from the SPELLSTRIKE project root. Re-running preserves existing .meta GUIDs.
"""
import copy
import hashlib
import json
import re
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/VFX/BasicAttackCharge'
OUT.mkdir(parents=True, exist_ok=True)

def guid(path):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'spellstrike/basic-charge/' + str(path).replace('\\', '/')).hex

def meta(path, importer='DefaultImporter', extra=''):
    path = Path(path)
    dest = ROOT / (str(path) + '.meta')
    if dest.exists():
        return re.search(r'guid: (\w+)', dest.read_text())[1]
    value = guid(path)
    dest.write_text(f'fileFormatVersion: 2\nguid: {value}\n{importer}:\n  externalObjects: {{}}\n{extra}  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
    return value

for path in ['Assets/Scripts/VFX', 'Assets/VFX', 'Assets/VFX/BasicAttackCharge']:
    p = ROOT / path
    p.mkdir(parents=True, exist_ok=True)
    if not Path(str(p) + '.meta').exists():
        Path(str(p) + '.meta').write_text(f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')

vfx_guid = meta('Assets/Scripts/VFX/BasicAttackChargeVfx.cs', 'MonoImporter', '  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n')
binding_guid = meta('Assets/Scripts/VFX/BasicAttackChargeBinding.cs', 'MonoImporter', '  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 100\n  icon: {instanceID: 0}\n')

# Retain an explicit pre-edit proof of the files that must remain unchanged.
evidence = ROOT / 'tmp/basic-charge-reference'
evidence.mkdir(parents=True, exist_ok=True)
baseline = evidence / 'protected-before.json'
protected = [
    'Assets/Scripts/AttackController.cs', 'Assets/Scripts/HomingProjectile.cs',
    'Assets/VAN AERT/ANIMS/BasicAttack.anim', 'Assets/VAN AERT/VanANIMCONTROLLER.controller',
    'Assets/Scenes/SampleScene.unity', 'Assets/Scenes/Stage2Scene/Stage2Scene.unity',
    'Assets/Scenes/TutorialScene.unity', 'Packages/manifest.json', 'Packages/packages-lock.json',
]
if not baseline.exists():
    baseline.write_text(json.dumps({p: hashlib.sha256((ROOT / p).read_bytes()).hexdigest() for p in protected}, indent=2))
    (evidence / 'VanAertPlayer.before.prefab.txt').write_bytes((ROOT / 'Assets/Prefabs/VanAertPlayer.prefab').read_bytes())

template = next((ROOT / 'Library/PackageCache').glob('com.unity.shadergraph@*/GraphTemplates/Cross Pipeline/Unlit Simple.shadergraph'))
decoder = json.JSONDecoder()
s = template.read_text().lstrip()
objects = []
while s:
    obj, end = decoder.raw_decode(s)
    objects.append(obj)
    s = s[end:].lstrip()
source = {o['m_ObjectId']: o for o in objects}
graph = copy.deepcopy(objects[0])
kept = []
for o in objects:
    if o.get('m_SerializedDescriptor') in ['SurfaceDescription.BaseColor', 'SurfaceDescription.Alpha']:
        kept.append(copy.deepcopy(o))
        kept.extend(copy.deepcopy(source[s['m_Id']]) for s in o['m_Slots'])
target = copy.deepcopy(next(o for o in objects if o['m_Type'].endswith('.UniversalTarget')))
target.update(m_SurfaceType=1, m_ZWriteControl=2, m_AlphaMode=2, m_RenderFace=0,
              m_CastShadows=False, m_ReceiveShadows=False, m_AllowMaterialOverride=False)
kept += [target, copy.deepcopy(source[target['m_ActiveSubTarget']['m_Id']])]
graph.update(m_Properties=[], m_Keywords=[], m_CategoryData=[], m_Edges=[],
             m_ActiveTargets=[{'m_Id': target['m_ObjectId']}], m_SubDatas=[],
             m_Path='SPELLSTRIKE/VFX', m_GraphPrecision=1)
graph['m_VertexContext']['m_Blocks'] = []
blocks = [o for o in kept if o['m_Type'].endswith('.BlockNode')]
graph['m_FragmentContext']['m_Blocks'] = [{'m_Id': o['m_ObjectId']} for o in blocks]
graph['m_Nodes'] = list(graph['m_FragmentContext']['m_Blocks'])

def slot(name, index, size, output):
    ident = uuid.uuid4().hex
    zero = 0.0 if size == 1 else {axis: 0.0 for axis in 'xyzw'[:size]}
    obj = dict(m_SGVersion=0, m_Type=f'UnityEditor.ShaderGraph.Vector{size}MaterialSlot',
               m_ObjectId=ident, m_Id=index, m_DisplayName=name, m_SlotType=int(output),
               m_Hidden=False, m_ShaderOutputName=name, m_StageCapability=3,
               m_Value=zero, m_DefaultValue=zero, m_Labels=[])
    kept.append(obj)
    return {'m_Id': ident}

def node(kind, name, x, y, slots, **fields):
    obj = copy.deepcopy(blocks[0])
    obj.pop('m_SerializedDescriptor')
    obj.update(m_Type='UnityEditor.ShaderGraph.' + kind, m_ObjectId=uuid.uuid4().hex,
               m_Name=name, m_Slots=slots, **fields)
    obj['m_DrawState']['m_Position'].update(x=x, y=y, width=210.0, height=170.0)
    kept.append(obj)
    graph['m_Nodes'].append({'m_Id': obj['m_ObjectId']})
    return obj['m_ObjectId']

uv = node('UVNode', 'UV', -660, 140, [slot('Out', 0, 4, True)], m_OutputChannel=0)
color = node('VertexColorNode', 'Vertex Color', -660, 360, [slot('Out', 0, 4, True)])
function = node('CustomFunctionNode', 'ChargeMask (Custom Function)', -330, 180,
                [slot('UV', 0, 4, False), slot('Tint', 1, 4, False),
                 slot('RGB', 2, 3, True), slot('Alpha', 3, 1, True)],
                m_SGVersion=1, m_SourceType=1, m_FunctionName='ChargeMask', m_FunctionSource='',
                m_FunctionBody='''$precision edge = saturate(1.0 - abs(UV.y * 2.0 - 1.0));
$precision2 p = UV.xy * 2.0 - 1.0;
$precision disc = saturate(1.0 - dot(p, p));
$precision ribbon = edge * edge;
$precision glow = disc * disc * disc;
RGB = Tint.rgb;
Alpha = saturate(lerp(ribbon, glow, step(0.5, UV.z)) * Tint.a);''')

def edge(a, out_slot, b, in_slot):
    graph['m_Edges'].append({'m_OutputSlot': {'m_Node': {'m_Id': a}, 'm_SlotId': out_slot},
                            'm_InputSlot': {'m_Node': {'m_Id': b}, 'm_SlotId': in_slot}})
edge(uv, 0, function, 0); edge(color, 0, function, 1)
for b in blocks:
    edge(function, 2 if b['m_SerializedDescriptor'].endswith('BaseColor') else 3, b['m_ObjectId'], 0)
graphpath = 'Assets/VFX/BasicAttackCharge/BasicChargeEnergy.shadergraph'
(ROOT / graphpath).write_text('\n\n'.join(json.dumps(o, indent=4) for o in [graph] + kept) + '\n')
graphguid = meta(graphpath, 'ScriptedImporter', '  serializedVersion: 2\n  userData: \n  script: {fileID: 11500000, guid: 625f186215c104763be7675aa2d941aa, type: 3}\n')
matpath = 'Assets/VFX/BasicAttackCharge/BasicChargeEnergy.mat'
(ROOT / matpath).write_text(f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: BasicChargeEnergy
  m_Shader: {{fileID: -6465566751694194690, guid: {graphguid}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties:
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs: []
    m_Ints: []
    m_Floats: []
    m_Colors: []
  m_BuildTextureStacks: []
''')
matguid = meta(matpath, 'NativeFormatImporter', '  mainObjectFileID: 2100000\n')

def component(file_id, owner, script_guid):
    return f'''--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {owner}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier:
'''

prefabpath = 'Assets/VFX/BasicAttackCharge/BasicAttackCharge.prefab'
(ROOT / prefabpath).write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 400000}
  - component: {fileID: 11400000}
  m_Layer: 0
  m_Name: BasicAttackCharge
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &400000
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100000}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
''' + component(11400000, 100000, vfx_guid) + f'''  energyMaterial: {{fileID: 2100000, guid: {matguid}, type: 2}}
  cyan: {{r: 0.08, g: 0.8, b: 1, a: 1}}
  chargeIntensity: 1
  streakDensity: 1
  orbitRadius: 0.34
  orbitSpeed: 7
  attractionStrength: 1.65
  streakWidth: 0.012
  ringRadius: 0.29
  ringEmission: 0.75
  ringWidth: 0.006
  coreIntensity: 5
  coreRadius: 0.047
  startSize: 0.18
  growthPower: 1.35
  quality: 0
  desktopStreaks: 12
  mobileStreaks: 8
  desktopMotes: 18
  mobileMotes: 10
''')
prefabguid = meta(prefabpath, 'PrefabImporter')
canonical = ROOT / 'Assets/Prefabs/VanAertPlayer.prefab'
text = canonical.read_text()
binding_id = 917170001337420001
if f'&{binding_id}' not in text:
    anchor = '  - component: {fileID: 589962891031320355}'
    assert text.count(anchor) == 1
    text = text.replace(anchor, anchor + f'\n  - component: {{fileID: {binding_id}}}')
    text += component(binding_id, 7206189519748278718, binding_guid) + f'''  playerAnimator: {{fileID: 6697293896239854505}}
  magicSpawnPoint: {{fileID: 8820908893504474642}}
  chargePrefab: {{fileID: 11400000, guid: {prefabguid}, type: 3}}
  basicAttackClip: {{fileID: 7400000, guid: d1e5b9899ab76884eb25c3a837866399, type: 2}}
  statePath: Base Layer.BasicAttack
  animatorLayer: 0
  postReleaseFadeSeconds: 0.16
'''
    canonical.write_text(text)
print('Authored charge Shader Graph/material/prefab; canonical player receives one visual-only component.')
