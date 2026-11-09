from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
p=Path('ArtSource/Stage3/Node1/Character/OriginalHands_Rework_v001');sheet=Image.new('RGB',(1440,786),(26,26,29));draw=ImageDraw.Draw(sheet);font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',30)
for i,(f,label) in enumerate([('Original_HandTop.png','Original'),('Final_HandTop.png','Reworked original')]):
 sheet.paste(Image.open(p/f).convert('RGB').resize((720,720)),(i*720,66));draw.text((i*720+24,16),label,font=font,fill=(238,238,238))
sheet.save(p/'Hand_Before_After.png')
