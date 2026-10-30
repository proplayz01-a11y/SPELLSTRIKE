from pathlib import Path
import cv2, numpy as np
from PIL import Image
root=Path(r"C:\Users\David\PROTOTYPE\SPELLSTRIKE\ArtSource\Cinematics\SpellstrikeSpellbook\Working\Textures")
root.mkdir(parents=True,exist_ok=True)
N=1024
rng=np.random.default_rng(43012)
def noise(sigma):
    ar=rng.random((N,N)).astype(np.float32)
    return cv2.GaussianBlur(ar,(0,0),sigmaX=sigma,sigmaY=sigma)
def norm(x):
    x=(x-x.mean())/(x.std()+1e-8)
    return np.clip(x,-2.4,2.4)
coarse=norm(noise(56));medium=norm(noise(15));fine=norm(noise(2.2))
grain=norm(noise(.55))
# Leather finish: deep indigo-black in the grain, dusty burnt umber worn patches.
def leather(name,base,brown):
    mask=np.clip(.5+.21*coarse+.09*medium+.03*fine,0,1)
    arr=np.empty((N,N,3),np.float32)
    for k in range(3):
        arr[:,:,k]=base[k]*(1-mask)+brown[k]*mask
    fibers=.8*medium+.50*fine+.21*grain
    arr+=fibers[:,:,None]*1.8
    # Sparse warm surface cracks, scattered well below lettering/ornament scale.
    wear=np.zeros((N,N),np.uint8)
    local=np.random.default_rng(19862)
    for i in range(400):
        x=int(local.integers(0,N));y=int(local.integers(0,N))
        length=int(local.integers(3,24))
        dx=int(length*.65);dy=int(-length*.35)
        cv2.line(wear,(x,y),(max(0,min(N-1,x+dx)),max(0,min(N-1,y+dy))),int(local.integers(6,25)),1,cv2.LINE_AA)
    arr+=wear[:,:,None]*np.array([.55,.25,.13],np.float32)
    image=Image.fromarray(np.clip(arr,0,255).astype(np.uint8),"RGB")
    path=root/name
    image.save(path,optimize=True)
    print(path.name,path.stat().st_size)
leather("Leather_AncientMidnight_1024.png",[10,14,20],[36,26,22])
leather("Leather_InsetCharcoal_1024.png",[9,13,18],[32,25,22])
leather("Leather_SpineBrown_1024.png",[30,21,16],[76,48,30])
# Warm aged parchment, tattered warm spots but no baked text; the 24 leaves
# retain independent full 0..1 page UVs for actual artwork later.
var=.70*coarse+.28*medium+.08*fine
spots=np.clip(.4+var*.22,0,1)
page=np.zeros((N,N,3),np.float32)
paper_base=np.array([195,169,127],np.float32)
paper_warm=np.array([145,114,77],np.float32)
page[:]=paper_base[None,None,:]*(1-spots[:,:,None])+paper_warm[None,None,:]*spots[:,:,None]
page+=(.55*fine+grain*.35)[:,:,None]*4
# Mild fiber strokes, not line art. Keep center readable.
for i in range(750):
    x=int(rng.integers(0,N));y=int(rng.integers(0,N))
    length=int(rng.integers(3,18))
    cv2.line(page,(x,y),(min(N-1,x+length),y),(.2,.22,.16),1)
pp=root/"Parchment_AgedBlank_1024.png"
Image.fromarray(np.clip(page,0,255).astype(np.uint8),"RGB").save(pp,optimize=True)
print(pp.name,pp.stat().st_size)
