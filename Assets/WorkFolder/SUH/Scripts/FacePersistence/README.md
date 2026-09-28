# Face persistence

- `SUH/Scene/FaceDraw`: `FaceSaveController` reads `IDataStoreService.GetAsync<uint>("BodyColor")`, selects the same six-material palette as the gameplay player, then restores the saved drawing over the current skin color, even when the skin index changed.
- Confirm/save waits for Cloud Save before firing `onConfirmed` or closing the editor. Failure keeps the canvas open. The status banner reports loading/saving/failure.
- UGS public player-data key `FaceAppearanceV1` now stores schema version 2, skin palette index, explicit background color, and a 512x512 PNG encoded as Base64. PNG contains both the opaque skin background and painted pixels (not editable vector strokes). This key is separate from the existing `V1_<playerId>` record to avoid uploading the image whenever score changes.
- FaceDraw and gameplay replace the stored background with the selected palette color without tinting other opaque stroke pixels. Legacy version 1 treats exact white pixels as background (old data cannot distinguish white strokes from white background); no cloud data is overwritten until the user saves. Version 2 records the background so intentional white strokes on colored skin remain white. Transparent pixels are composited over opaque skin.
- `SUH/Scene/New/MainScene` already spawns `PAP/Prefabs/Players/Player (1).prefab`. Its new `PlayerSavedFace` component resolves the existing Fusion `[Networked] Player.playerId` and loads that player's public face on every peer, including late joiners. No additional RPC or NetworkBehaviour bake is required. `Spawner` preloads the local face before spawning.
- Each player owns an independent face material/texture at slot 1; body slot 0 remains controlled by the existing body-color logic. Failed loads retry after 10 seconds. Successful reads are cached and invalidated when the character is destroyed.
- Gameplay uses each owner's current Fusion `NetWorkedColor`, not the face record's historical `BodyColor`. It waits for the initial body color and recomposes from the original cached PNG whenever the replicated color changes. This does not refetch or rewrite UGS face data. Body-color-only changes do not require saving the face again.
- Existing baked PNGs only identify background by exact pixel color; antialiased stroke edges retain their old blended color, and strokes identical to the background cannot be distinguished. This is not a separate transparent stroke layer.
- An already-spawned remote character does not poll for cloud edits; the requested flow publishes changes when returning to MainScene and creating a character.

## Validation

`Tools > Face Persistence > Validate Setup` checks scene/prefab references, six palette entries, face UV projection and exact skin/line pixel recovery through PNG + JSON. Passed in Unity during implementation; C# runtime/editor builds passed.

Actual authenticated save/relaunch and two-client Shared Mode checks still require an online play test. Test saving a drawing, reloading FaceDraw, spawning in MainScene, and joining from a second client after the first has spawned. Offline save must keep the editing panel open with an error.
