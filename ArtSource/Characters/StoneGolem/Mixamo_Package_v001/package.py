from pathlib import Path
import zipfile
p=Path('ArtSource/Characters/StoneGolem/Mixamo_Package_v001')
with zipfile.ZipFile(p/'StoneGolem_Mixamo_Package.zip','w',zipfile.ZIP_DEFLATED) as z:
 for name in ['StoneGolem_Mixamo.fbx','StoneGolem_Mixamo_Copy.blend','Textures/StoneGolem_BaseColor.jpg','StoneGolem_Mixamo_Front.png','README.md','Export_Verification.json']:z.write(p/name,name)
print((p/'StoneGolem_Mixamo_Package.zip').stat().st_size)
