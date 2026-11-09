from pathlib import Path
p=Path('ArtSource/Characters/StoneGolem/VolumetricStone_v001/build_stones.py');s=p.read_text().replace("[0,-.03,1.76]","[0,-.12,1.65]").replace("weights[0]=.030","weights[0]=.035").replace("neighbors=np.argsort(np.sum((seeds-seed)**2,axis=1))[1:25]","neighbors=list(np.argsort(np.sum((seeds-seed)**2,axis=1))[1:25])\n if i!=0 and 0 not in neighbors:neighbors.append(0)")
p.with_name('build_stones_v2.py').write_text(s)
p=Path('ArtSource/Characters/StoneGolem/VolumetricStone_v001/refine_and_render.py');s=p.read_text().replace("core.data=source.data.copy()","core.data=bpy.data.objects['AUTHOR_Watertight_Fracture_Base'].data.copy()")
p.with_name('refine_and_render_v2.py').write_text(s)
