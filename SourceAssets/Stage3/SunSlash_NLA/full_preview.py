from PIL import Image
from pathlib import Path
p=Path('SourceAssets/Stage3/SunSlash_NLA/Full158');ims=[Image.open(f).convert('RGB').resize((480,520)) for f in sorted(p.glob('Frame_*.png'))];ims[0].save(p/'SunSlash_Full158_Preview.gif',save_all=True,append_images=ims[1:],duration=[30,30,40]*52+[30,40],loop=0)

