"""Pre-blurred grid backgrounds (tvOS): small blurred 320x180 images,
enlarged by the screen = a blur effect with no live computation.
Usage: python3 tools/art/make-blur.py  (from the repository root)"""
from PIL import Image, ImageFilter, ImageEnhance
import glob, os
os.chdir(os.path.join(os.path.dirname(__file__), '..', '..', 'launcher', 'images'))
os.makedirs('blur', exist_ok=True)
for f in sorted(glob.glob('bg-*.jpg')) + sorted(glob.glob('apps/*-bg.jpg')):
    im = Image.open(f).convert('RGB').resize((320, 180), Image.LANCZOS).filter(ImageFilter.GaussianBlur(7))
    im = ImageEnhance.Color(ImageEnhance.Brightness(im).enhance(0.72)).enhance(1.25)
    name = os.path.basename(f).replace('-bg.jpg', '').replace('.jpg', '').replace('bg-', '')
    im.save(f'blur/{name}.jpg', quality=72, optimize=True)
    print('ok', name)
