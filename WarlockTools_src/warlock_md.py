# Warlock locdata.md (mdmp) — not Majesty layout.
# UTF-8 string pool, 16641-mod hash, 4 slots/bucket, overflow list.

import struct
import hashlib
from pathlib import Path

MAGIC = 0x706D646D
STAMP = 0xC4194
CODEPAGE = 1251
BLOCK1_CAP = 13313
BUCKETS_MOD = 16641
BUCKETS_STORE = 16640
MAX_SLOT = 4
OV_CAP = 64
PTR_BASE = 0x34024
HASH_HDR = 0x34020
BUCKET0 = 0x3402C
OV_OFF = 0x117864
POOL_USED_OFF = 0x117B6C
POOL_CAP_OFF = 0x117B70
POOL_OFF = 0x117B74
POOL_CAP = 0xC0000
PAYLOAD = 0x1D7B80  # bytes after magic the exe reads


def _u32(d, o):
    return struct.unpack_from("<I", d, o)[0]


def _i32(d, o):
    return struct.unpack_from("<i", d, o)[0]


def get_key(s: bytes) -> int:
    h = 0
    for b in s:
        c = b if b < 128 else b - 256
        h = h * 31 + c
        h = ((h + 0x80000000) & 0xFFFFFFFF) - 0x80000000
    return (h & 0xFFFFFFFF) % BUCKETS_MOD


def _resolve(d, index):
    slot = index // BUCKETS_MOD
    buck = index % BUCKETS_MOD
    if slot < MAX_SLOT:
        off = BUCKET0 + buck * 56
        sl = off + 8 + slot * 12
        ptr, ln = _i32(d, sl), _i32(d, sl + 4)
    else:
        sl = OV_OFF + 8 + (slot - MAX_SLOT) * 12
        ptr, ln = _i32(d, sl), _i32(d, sl + 4)
    fo = ptr + PTR_BASE
    return d[fo : fo + ln]


def unpack_pairs(data: bytes):
    if _u32(data, 0) != MAGIC:
        raise ValueError("not mdmp")
    if _u32(data, 4) != STAMP:
        raise ValueError("unexpected header stamp 0x%X" % _u32(data, 4))
    n = _i32(data, 0x0C)
    pairs = []
    for i in range(n):
        o = 0x10 + i * 16
        name = _resolve(data, _i32(data, o + 4))
        val = _resolve(data, _i32(data, o + 12))
        pairs.append((name, val))
    return pairs


def unpack_to_text(md_path, txt_path):
    data = Path(md_path).read_bytes()
    pairs = unpack_pairs(data)
    lines = ["%08x %08x" % (STAMP, CODEPAGE)]
    for k, v in pairs:
        ks = k.decode("utf-8")
        vs = v.decode("utf-8").replace("\r\n", "\n").replace("\n", "\\n")
        lines.append("%s=%s" % (ks, vs))
    Path(txt_path).write_text("\n".join(lines) + "\n", encoding="utf-8")
    return len(pairs)


class _Table:
    def __init__(self):
        self.buckets = [[ ] for _ in range(BUCKETS_STORE)]  # list of (ptr, ln, idx)
        self.overflow = []
        self.pool = bytearray(b"\x00")  # empty string at 0
        self.intern = {}  # bytes -> encoded index
        self.ptr0 = POOL_OFF - PTR_BASE  # 0xE3B50

    def add(self, s: bytes, pair_idx: int) -> int:
        if s in self.intern:
            return self.intern[s]
        ptr = self.ptr0 + len(self.pool)
        self.pool += s + b"\x00"
        ln = len(s)
        buck = get_key(s)
        idx_field = pair_idx if s.startswith(b"#") else -1
        rec = (ptr, ln, idx_field)
        if buck < BUCKETS_STORE and len(self.buckets[buck]) < MAX_SLOT:
            slot = len(self.buckets[buck])
            self.buckets[buck].append(rec)
            enc = buck + slot * BUCKETS_MOD
        else:
            slot = MAX_SLOT + len(self.overflow)
            self.overflow.append(rec)
            enc = buck + slot * BUCKETS_MOD
        self.intern[s] = enc
        return enc


def pack_pairs(pairs):
    if len(pairs) > BLOCK1_CAP:
        raise ValueError("too many pairs: %d > %d" % (len(pairs), BLOCK1_CAP))
    t = _Table()
    block1 = []
    for i, (k, v) in enumerate(pairs):
        ni = t.add(k, i)
        vi = t.add(v, i)
        name_idx0 = BLOCK1_CAP if i == 0 else 0
        block1.append((name_idx0, ni, 0, vi))
    if len(t.overflow) > OV_CAP:
        raise ValueError("overflow overflow: %d > %d" % (len(t.overflow), OV_CAP))
    if len(t.pool) > POOL_CAP:
        raise ValueError("pool too big: %d > %d" % (len(t.pool), POOL_CAP))

    buf = bytearray(4 + PAYLOAD)
    struct.pack_into("<I", buf, 0, MAGIC)
    struct.pack_into("<I", buf, 4, STAMP)
    struct.pack_into("<I", buf, 8, CODEPAGE)
    struct.pack_into("<I", buf, 0x0C, len(pairs))
    for i in range(BLOCK1_CAP):
        o = 0x10 + i * 16
        if i < len(block1):
            a, b, c, d = block1[i]
        else:
            a, b, c, d = 0, -1, 0, -1
        struct.pack_into("<iiii", buf, o, a, b, c, d)
    struct.pack_into("<I", buf, HASH_HDR, 0)
    struct.pack_into("<I", buf, HASH_HDR + 4, BUCKETS_MOD)
    struct.pack_into("<I", buf, HASH_HDR + 8, BUCKETS_MOD)
    for b in range(BUCKETS_STORE):
        o = BUCKET0 + b * 56
        slots = t.buckets[b]
        struct.pack_into("<ii", buf, o, len(slots), MAX_SLOT)
        for s in range(MAX_SLOT):
            sl = o + 8 + s * 12
            if s < len(slots):
                ptr, ln, ix = slots[s]
                struct.pack_into("<iii", buf, sl, ptr, ln, ix)
            else:
                struct.pack_into("<iii", buf, sl, 0, 0, -1)
    struct.pack_into("<ii", buf, OV_OFF, len(t.overflow), OV_CAP)
    for s in range(OV_CAP):
        sl = OV_OFF + 8 + s * 12
        if s < len(t.overflow):
            ptr, ln, ix = t.overflow[s]
            struct.pack_into("<iii", buf, sl, ptr, ln, ix)
        else:
            struct.pack_into("<iii", buf, sl, 0, 0, -1)
    used = len(t.pool)
    struct.pack_into("<I", buf, POOL_USED_OFF, used)
    struct.pack_into("<I", buf, POOL_CAP_OFF, POOL_CAP)
    buf[POOL_OFF : POOL_OFF + used] = t.pool
    return bytes(buf)


def pack_from_text(txt_path, md_path):
    text = Path(txt_path).read_text(encoding="utf-8")
    pairs = []
    for line in text.splitlines():
        if not line.strip():
            continue
        if line[0] != "#" and " " in line[:20] and "=" not in line[:20]:
            continue  # header "000c4194 000004e3"
        if "=" not in line:
            continue
        k, v = line.split("=", 1)
        v = v.replace("\\n", "\n")
        pairs.append((k.encode("utf-8"), v.encode("utf-8")))
    data = pack_pairs(pairs)
    Path(md_path).write_bytes(data)
    return len(pairs), len(data)


if __name__ == "__main__":
    src = Path(r"E:\Steam\steamapps\common\Warlock - Master of the Arcane\modding.tools\_extract\l100\locdata.md")
    tmp = src.with_name("locdata_roundtrip.md")
    txt = src.with_name("locdata_dec.txt")
    n = unpack_to_text(src, txt)
    print("unpacked", n, "->", txt)
    n2, sz = pack_from_text(txt, tmp)
    print("packed", n2, "bytes", sz)
    a = src.read_bytes()
    b = tmp.read_bytes()
    print("orig", len(a), hashlib.md5(a).hexdigest())
    print("new ", len(b), hashlib.md5(b).hexdigest())
    if a[: len(b)] == b or a == b:
        print("IDENTICAL (or prefix)")
    else:
        diffs = 0
        m = min(len(a), len(b))
        first = None
        for i in range(m):
            if a[i] != b[i]:
                diffs += 1
                if first is None:
                    first = i
        print("diffs", diffs, "first", hex(first) if first is not None else None)
        if first is not None:
            print(" orig", a[first : first + 32].hex())
            print(" new ", b[first : first + 32].hex())
