"""Fetches uBlock Origin Lite, the ad blocker the companion app loads into its browser.

The extension is not kept in git (34 MB of filter lists, made by someone else): this
downloads one pinned release from the project's GitHub page, checks it against the hash
below and unpacks it to build/ubol/ext, where the app project picks it up as "adblock".
Run again after changing VERSION and SHA256; a matching copy is left alone.

uBlock Origin Lite: https://github.com/uBlockOrigin/uBOL-home (GPLv3, Raymond Hill).
"""
import hashlib
import io
import shutil
import sys
import urllib.request
import zipfile
from pathlib import Path

VERSION = '2026.1116.1650'
SHA256 = '67e084b34470c51c08fa9314b118b8cb73df3a72e90c6a3b536d041c92e61bbc'
URL = ('https://github.com/uBlockOrigin/uBOL-home/releases/download/'
       f'{VERSION}/uBOLite_{VERSION}.chromium.zip')

ROOT = Path(__file__).resolve().parent.parent
OUT = ROOT / 'build' / 'ubol'
EXT = OUT / 'ext'
STAMP = OUT / 'version.txt'


def main() -> int:
    if STAMP.is_file() and STAMP.read_text(encoding='utf-8').strip() == VERSION and (EXT / 'manifest.json').is_file():
        print(f'uBlock Origin Lite {VERSION}: already there')
        return 0
    print(f'uBlock Origin Lite {VERSION}: downloading')
    request = urllib.request.Request(URL, headers={'User-Agent': 'CabinPlay-build'})
    with urllib.request.urlopen(request, timeout=120) as response:
        data = response.read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != SHA256:
        print(f'uBlock Origin Lite: wrong hash {digest}, expected {SHA256}', file=sys.stderr)
        return 1
    if EXT.exists():
        shutil.rmtree(EXT)
    EXT.mkdir(parents=True)
    with zipfile.ZipFile(io.BytesIO(data)) as archive:
        for name in archive.namelist():
            target = (EXT / name).resolve()
            if not target.is_relative_to(EXT.resolve()):
                print(f'uBlock Origin Lite: bad path in the archive: {name}', file=sys.stderr)
                return 1
        archive.extractall(EXT)
    STAMP.write_text(VERSION + '\n', encoding='utf-8')
    print(f'uBlock Origin Lite {VERSION}: unpacked to {EXT}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
