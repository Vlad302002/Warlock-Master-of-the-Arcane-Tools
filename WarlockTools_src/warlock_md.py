# Warlock / Warlock 2 locdata.md (mdmp) — not Majesty layout.
# W1: stamp 0xC4194, 16641-mod, 4 slots/bucket
# W2: stamp 0x101F70, 8191-mod, 7 slots/bucket
# UTF-8 string pool.

import struct
import hashlib
from pathlib import Path

MAGIC = 0x706D646D

LAYOUTS = {
    0xC4194: dict(  # Warlock 1
        name="Warlock1",
        stamp=0xC4194,
        default_cp=1251,
        block1_cap=13313,
        buckets_mod=16641,
        buckets_store=16640,
        max_slot=4,
        ov_cap=64,
        ptr_base=0x34024,
        hash_hdr=0x34020,
        bucket0=0x3402C,
        bucket_size=56,
        ov_off=0x117864,
        pool_used_off=0x117B6C,
        pool_cap_off=0x117B70,
        pool_off=0x117B74,
        pool_cap=0xC0000,
        payload=0x1D7B80,
    ),
    0x101F70: dict(  # Warlock 2
        name="Warlock2",
        stamp=0x101F70,
        default_cp=1251,
        block1_cap=12288,
        buckets_mod=8191,
        buckets_store=8191,
        max_slot=7,
        ov_cap=100,
        ptr_base=0x30014,
        hash_hdr=0x30010,
        bucket0=0x3001C,
        bucket_size=92,
        ov_off=0xE7FC0,
        pool_used_off=0xE8478,
        pool_cap_off=0xE847C,
        pool_off=0xE8480,
        pool_cap=0x100000,
        # 4 + payload = 2000020 (original l100 size; 16 B after pool region)
        payload=0x1E8490,
    ),
}


def _u32(d, o):
    return struct.unpack_from("<I", d, o)[0]


def _i32(d, o):
    return struct.unpack_from("<i", d, o)[0]


def _layout(stamp):
    if stamp not in LAYOUTS:
        raise ValueError("unexpected mdmp stamp 0x%X (known: W1=0xC4194, W2=0x101F70)" % stamp)
    return LAYOUTS[stamp]


def get_key(s: bytes, buckets_mod: int) -> int:
    h = 0
    for b in s:
        c = b if b < 128 else b - 256
        h = h * 31 + c
        h = ((h + 0x80000000) & 0xFFFFFFFF) - 0x80000000
    return (h & 0xFFFFFFFF) % buckets_mod


def _resolve(d, index, L):
    slot = index // L["buckets_mod"]
    buck = index % L["buckets_mod"]
    if slot < L["max_slot"]:
        sl = L["bucket0"] + buck * L["bucket_size"] + 8 + slot * 12
    else:
        sl = L["ov_off"] + 8 + (slot - L["max_slot"]) * 12
    ptr, ln = _i32(d, sl), _i32(d, sl + 4)
    fo = ptr + L["ptr_base"]
    if ln < 0 or fo < 0 or fo + ln > len(d):
        raise ValueError("bad string ptr index=%s ptr=%s ln=%s fo=%#x" % (index, ptr, ln, fo))
    return d[fo : fo + ln]


def unpack_pairs(data: bytes):
    if _u32(data, 0) != MAGIC:
        raise ValueError("not mdmp")
    L = _layout(_u32(data, 4))
    n = _i32(data, 0x0C)
    pairs = []
    for i in range(n):
        o = 0x10 + i * 16
        name = _resolve(data, _i32(data, o + 4), L)
        val = _resolve(data, _i32(data, o + 12), L)
        pairs.append((name, val))
    return L, pairs


def unpack_to_text(md_path, txt_path):
    data = Path(md_path).read_bytes()
    L, pairs = unpack_pairs(data)
    cp = _u32(data, 8)
    lines = ["%08x %08x" % (L["stamp"], cp)]
    for k, v in pairs:
        ks = k.decode("utf-8")
        # Normalize CR/LF so bare \r cannot split lines on re-pack.
        vs = v.decode("utf-8").replace("\r\n", "\n").replace("\r", "\n").replace("\n", "\\n")
        lines.append("%s=%s" % (ks, vs))
    Path(txt_path).write_text("\n".join(lines) + "\n", encoding="utf-8")
    return len(pairs), L["name"]


class _Table:
    def __init__(self, L):
        self.L = L
        self.buckets = [[] for _ in range(L["buckets_store"])]
        self.overflow = []
        self.pool = bytearray(b"\x00")
        self.intern = {}
        self.ptr0 = L["pool_off"] - L["ptr_base"]

    def add(self, s: bytes, pair_idx: int) -> int:
        if s in self.intern:
            return self.intern[s]
        ptr = self.ptr0 + len(self.pool)
        self.pool += s + b"\x00"
        ln = len(s)
        buck = get_key(s, self.L["buckets_mod"])
        idx_field = pair_idx if s.startswith(b"#") else -1
        rec = (ptr, ln, idx_field)
        if buck < self.L["buckets_store"] and len(self.buckets[buck]) < self.L["max_slot"]:
            slot = len(self.buckets[buck])
            self.buckets[buck].append(rec)
        else:
            slot = self.L["max_slot"] + len(self.overflow)
            self.overflow.append(rec)
        enc = buck + slot * self.L["buckets_mod"]
        self.intern[s] = enc
        return enc


def pack_pairs(pairs, L, codepage=None):
    if codepage is None:
        codepage = L["default_cp"]
    if len(pairs) > L["block1_cap"]:
        raise ValueError("too many pairs: %d > %d" % (len(pairs), L["block1_cap"]))
    t = _Table(L)
    block1 = []
    for i, (k, v) in enumerate(pairs):
        ni = t.add(k, i)
        vi = t.add(v, i)
        name_idx0 = L["block1_cap"] if i == 0 else 0
        block1.append((name_idx0, ni, 0, vi))
    if len(t.overflow) > L["ov_cap"]:
        raise ValueError("overflow overflow: %d > %d" % (len(t.overflow), L["ov_cap"]))
    if len(t.pool) > L["pool_cap"]:
        raise ValueError("pool too big: %d > %d" % (len(t.pool), L["pool_cap"]))

    buf = bytearray(4 + L["payload"])
    struct.pack_into("<I", buf, 0, MAGIC)
    struct.pack_into("<I", buf, 4, L["stamp"])
    struct.pack_into("<I", buf, 8, codepage)
    struct.pack_into("<I", buf, 0x0C, len(pairs))
    for i in range(L["block1_cap"]):
        o = 0x10 + i * 16
        if i < len(block1):
            a, b, c, d = block1[i]
        else:
            a, b, c, d = 0, -1, 0, -1
        struct.pack_into("<iiii", buf, o, a, b, c, d)
    struct.pack_into("<I", buf, L["hash_hdr"], 0)
    struct.pack_into("<I", buf, L["hash_hdr"] + 4, L["buckets_mod"])
    struct.pack_into("<I", buf, L["hash_hdr"] + 8, L["buckets_mod"])
    for b in range(L["buckets_store"]):
        o = L["bucket0"] + b * L["bucket_size"]
        slots = t.buckets[b]
        struct.pack_into("<ii", buf, o, len(slots), L["max_slot"])
        for s in range(L["max_slot"]):
            sl = o + 8 + s * 12
            if s < len(slots):
                ptr, ln, ix = slots[s]
                struct.pack_into("<iii", buf, sl, ptr, ln, ix)
            else:
                struct.pack_into("<iii", buf, sl, 0, 0, -1)
    struct.pack_into("<ii", buf, L["ov_off"], len(t.overflow), L["ov_cap"])
    for s in range(L["ov_cap"]):
        sl = L["ov_off"] + 8 + s * 12
        if s < len(t.overflow):
            ptr, ln, ix = t.overflow[s]
            struct.pack_into("<iii", buf, sl, ptr, ln, ix)
        else:
            struct.pack_into("<iii", buf, sl, 0, 0, -1)
    used = len(t.pool)
    struct.pack_into("<I", buf, L["pool_used_off"], used)
    struct.pack_into("<I", buf, L["pool_cap_off"], L["pool_cap"])
    buf[L["pool_off"] : L["pool_off"] + used] = t.pool
    # W2 originals end with a tiny locale tag after the pool region.
    if L["stamp"] == 0x101F70:
        fo = L["pool_off"] + L["pool_cap"]  # 0x1E8480
        tag = b"en" if codepage == 1252 else b"ru"
        struct.pack_into("<II", buf, fo, 2, 8)
        buf[fo + 8 : fo + 8 + len(tag)] = tag
    return bytes(buf)


def pack_from_text(txt_path, md_path):
    text = Path(txt_path).read_text(encoding="utf-8")
    pairs = []
    L = LAYOUTS[0xC4194]
    codepage = L["default_cp"]
    layout_set = False
    for line in text.splitlines():
        if not line.strip():
            continue
        if not layout_set and line[0] != "#" and "=" not in line[:24]:
            parts = line.split()
            if parts:
                stamp = int(parts[0], 16)
                L = _layout(stamp)
                layout_set = True
            if len(parts) >= 2:
                codepage = int(parts[1], 16)
            continue
        if "=" not in line:
            continue
        k, v = line.split("=", 1)
        v = v.replace("\\n", "\n")
        pairs.append((k.encode("utf-8"), v.encode("utf-8")))
    data = pack_pairs(pairs, L, codepage)
    Path(md_path).write_bytes(data)
    return len(pairs), len(data), L["name"]


if __name__ == "__main__":
    base = Path(r"C:\Program Files (x86)\R.G. Gamblers\Warlock 2\Tools Warlock modding\l100")
    for name in ("locdata.md", "locdataen.md"):
        src = base / name
        txt = base / (src.stem + ".txt")
        tmp = base / (src.stem + "_roundtrip.md")
        n, game = unpack_to_text(src, txt)
        print("unpacked", name, n, game, "->", txt.name)
        n2, sz, game2 = pack_from_text(txt, tmp)
        print("packed", n2, "bytes", sz, game2)
        a = src.read_bytes()
        b = tmp.read_bytes()
        print("orig", len(a), hashlib.md5(a).hexdigest())
        print("new ", len(b), hashlib.md5(b).hexdigest())
        if a == b:
            print("IDENTICAL")
        else:
            diffs = sum(1 for i in range(min(len(a), b and len(b))) if a[i] != b[i])
            first = next((i for i in range(min(len(a), len(b))) if a[i] != b[i]), None)
            print("diffs", diffs, "first", hex(first) if first is not None else None, "len", len(a), len(b))
        print()
