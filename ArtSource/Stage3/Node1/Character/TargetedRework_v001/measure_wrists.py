import bpy,pathlib,json
O=pathlib.Path('C:/Users/David/PROTOTYPE/SPELLSTRIKE/ArtSource/Stage3/Node1/Character/TargetedRework_v001');bpy.ops.wm.open_mainfile(filepath=str(O/'Source_Inspection.blend'));m=bpy.data.objects['model'].data
for x in [.77,.78,.79,.80,.81,.82,.83,.84,.85]:
 vs=[v.co for v in m.vertices if abs(v.co.x-x)<.005]
 print(x,len(vs),[[round(min(v[i] for v in vs),5),round(max(v[i] for v in vs),5)] for i in [1,2]])
