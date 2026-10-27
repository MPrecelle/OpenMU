# C2 FA - LockedContent (by server)

## Is sent when

The player entered the game world. Not part of the original protocol: it's a custom packet of Torvik MU, sent when content of the server is locked until a future release.

## Causes the following actions on the client side

The client hides the user interface of the locked features, e.g. the warp list entries of the locked maps. A client which doesn't receive this packet shows everything.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFA  | Packet header - packet type identifier |
| 4 | 1 | Boolean |  | IsGensLocked |
| 4 << 1 | 1 bit | Boolean |  | IsCashShopLocked |
| 5 | 1 | Byte |  | LockedMapCount |
| 6 | LockedMap.Length * LockedMapCount | Array of LockedMap |  | LockedMaps |

### LockedMap Structure

A map which is locked until a future release.

Length: 2 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 2 | ShortLittleEndian |  | MapNumber |