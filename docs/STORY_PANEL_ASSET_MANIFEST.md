# SPELLSTRIKE Story Panel Asset Manifest

**Purpose:** Canonical inventory of submitted story-panel assets and their
approval, continuity, and Unity-readiness status.

## Status Definitions

- **SOURCE APPROVED** — story content, character identity, composition, and
  emotional direction are approved. Preserve the exact original file.
- **UNITY FINAL APPROVED** — approved source has also passed the required
  16:9, resolution, caption-safe, and Unity import checks.
- **CANDIDATE** — pending explicit user approval.
- **REJECTED** — cannot be used as a generation or continuity reference.
- **REFERENCE ONLY** — establishes only the attributes listed in the manifest.

## Revision Visual Authority

For all currently unapproved later prologue panels, use the **art style** of
approved Panel 11 as the primary style reference because it is consistent with
the earlier approved panels. This approval is for Panel 11's visual treatment,
not an authorization to reuse its library environment, composition, dialogue,
or character blocking. The replacement library environment remains a separate
unresolved design decision.

## Canonical Reference Asset Registry

| Reference | Status | Controls | Does not control |
| --- | --- | --- | --- |
| `docs/story-references/SPELLSTRIKE_Global_Visual_Rules_REFERENCE.pdf` | REFERENCE ONLY; 6 pages; SHA-256 `729b965ccf627cc39b54d8b852ab21b7ac64f5ddbe6f3de298afb52b8ce37abc` | Global 16:9 rule, cinematic fantasy-comic presentation, bold ink-like outlines, painterly/cel-shaded rendering, palette roles, character identity sheets, and Van Aert's staff specification. | Final panel story beats, dialogue, final library composition, or permission to reuse a rejected candidate image. |
| `Assets/Story/References/Library_TopDown_Layout_REFERENCE.png` | CANONICAL SPATIAL-LAYOUT REFERENCE; 1672 × 941 RGB; SHA-256 `3ad6ab2c3bc07d4a81b588a3c3c56d6fd5c35a3884c686bd014d7fe8462c3dc4` | Final library layout: exterior door at the lower right; stained-glass window and blue banners at the lower center; main study table in the front room; flanking shelving; central arch/threshold; red-carpet route; and rear archive room. Future library panels must preserve these relationships. | Final art style, lighting, camera angle, character placement, dialogue, or a requirement to recreate the image's isometric top-down view. |
| `Assets/Story/References/Library_Left_Oblique_CAMERA_REFERENCE.png` | CAMERA REFERENCE ONLY; 1672 × 941 RGB; SHA-256 `4c284fd87e23510dd7de1228b453e95b2b9bcad1460df97ac1aceb5423a5798b` | Left-oblique perspective, depth, warm-study/cool-archive lighting separation, and side-on readability of the Gothic gateway. | Canonical architecture or object placement; the top-down layout wins every conflict. |
| `Assets/Story/Approved/Prologue_Panel_11_SOURCE_APPROVED.png` | SOURCE APPROVED | The art-style target for revised later panels: readable warm/cool contrast, clean confident outlines, controlled painterly-cel rendering, and magical cyan/gold accents compatible with Panels 1-4. | The final library layout, exact dialogue, character blocking, or all story beats. |
| `Assets/Story/References/Vander_Poel_Character_Reference_FINAL.jpg` | CANONICAL CHARACTER REFERENCE; 1920 × 1080 RGB; SHA-256 `f82cd3e3633a5c39e5d8dcaa5bb75c2b6add25b95320b687324c58428fefb0cd` | Vander Poel's illustrated identity: loose white/grey hair, broad white beard, blue eyes, glasses, teal mantle with gold trim, blue diamond chest gem, and aged scholarly presence. | Final scene composition, library environment, dialogue, or permission to reuse rejected candidate panels. |
| `Assets/Story/References/Vander_Poel_Staff_360_REFERENCE.png` | CANONICAL OBJECT REFERENCE; source approved; 1672 × 941 RGB; SHA-256 `983456a612a75d9ed5ee2c787f8cb74c5dc529c5a4f562b071a4afe91323c567` | Vander's distinct staff: heavier twisted dark-wood shaft, aged-gold fittings, circular cage, and one pale oval crystal. Mandatory reference for every panel containing Vander's staff. Locked scene scale: approximately 1.0× Vander's standing height, ferrule at floor level, cage top at his crown, unchanged component proportions. | Van Aert's staff, character pose, environment, story beat, magical VFX, permission to redesign the circular cage/crystal, or permission to make the staff tower above Vander. |
| `Assets/Story/References/Young_Van_Character_Reference_FINAL.png` | CANONICAL CHARACTER REFERENCE; source approved; 1536 × 1024 RGB; SHA-256 `13a8ed3a8579269db75cd93f59d467b2b67378e9f96d8d3b9c3140d421328a12` | Mandatory Younger Van identity: late-30s clean-shaven black-haired adult father, mature facial lineage from Older Van, four aligned full-body views, detailed Panel 8-style cel shading, and canonical navy-and-gold robe. Load this reference for every Younger Van replacement or new panel. | Does not control story blocking, environment, staff, dialogue, or camera composition; each approved panel retains those authorities. |
| `Assets/Story/References/Older_Van_Character_Reference_CANDIDATE_V1.png` | CANDIDATE CHARACTER REFERENCE; pending user approval; 1672 × 941 RGB; SHA-256 `41f3a2e332958a315b812ea212231ce5faeb5a45a3dfba346b6a91d00dd7ca92` | Proposed present-day Older Van identity: four aligned full-body views plus facial insets, half-tied silver hair with navy tie, tapered white beard, gold-rim glasses, blue eyes, lean capable aged build, detailed cel shading, navy-and-gold robe, circular clasp, hood, back embroidery and boots. | No authority until explicitly approved; does not include or control the separate canonical staff, story blocking, environment, dialogue, or camera composition. |
| `Assets/Story/References/Older_Van_3D_Modeling_Orthographic_CANDIDATE_V2.png` | CANDIDATE 3D MODELING REFERENCE; pending user approval; 1672 × 941 RGB; SHA-256 `4897416a4a0e2c63d57f1d6990037c31e0c6f1b792c94a1d0c3f2d2ee05683d4` | Clean orthographic front, strict side and back construction views on one baseline, with separated arm silhouettes and flat reference lighting. V2 removes the entire lower detail-inset strip and retains only the three full-body modeling views. | Not a rig, mesh, texture map, UV layout or Unity-ready model; staff remains a separate canonical prop reference. V1 remains archived as the rejected inset-heavy iteration. |
| `Assets/Story/References/Older_Van_3D_Front_TPose_FINAL.png` | CANONICAL SINGLE-VIEW 3D MODELING REFERENCE; approved; 1254 × 1254 RGB; SHA-256 `adfb8264d3eb2c04e04e6e736e3615376f3473b6261914e0605d1894dbc1acf1` | One strict front orthographic Older Van in a symmetrical T-pose, horizontal arms at shoulder height, palms down, full body centered on vertical centerline and floor baseline, with unobstructed fingertips, robe, inner clothing and boots. | Front view only; not a rig, mesh, UV layout, texture map or Unity-ready model. Staff remains separate. |
| `Assets/Story/References/Older_Van_3D_Side_TPose_FINAL.png` | CANONICAL SINGLE-VIEW 3D MODELING REFERENCE; approved; 1009 × 1559 RGB; SHA-256 `88ee8f8c3da04afaddf29d9124a3e7d46d9248830148b8cc5c5b87574bbee077` | One strict 90-degree left-side orthographic Older Van facing screen-right, with the T-pose arms correctly foreshortened end-on at shoulder height, plus measurable head, beard, hood, robe and boot depth. | Side view only; not a rig, mesh, UV layout, texture map or Unity-ready model. |
| `Assets/Story/References/Older_Van_3D_Back_TPose_FINAL.png` | CANONICAL SINGLE-VIEW 3D MODELING REFERENCE; approved; 1254 × 1254 RGB; SHA-256 `b7fadc0aa6d9758e55a9809e36b61a96286ac756c2c6150d7cb5f5a059e8ecd6` | One strict back orthographic Older Van matching the approved T-pose height/arm span, with centered half-tied hair, hood, symmetrical sleeves, central back embroidery, hem and rear boots. | Back view only; not a rig, mesh, UV layout, texture map or Unity-ready model. |
| `Assets/Story/References/Van_Staff_3D_Front_FINAL.png` | CANONICAL SINGLE-VIEW STAFF MODELING REFERENCE; approved; 1024 × 1536 RGB; SHA-256 `a625a3ea0867dd8d701e1cbf370baf93e329ae10d2813ef8146ba3acc99dc616` | One strict front orthographic Van staff with total tip-to-ferrule height locked to 1.00× Older Van's standing height; exact cyan crystal, aged-gold cradle, long straight dark-wood shaft and compact ferrule. | Front view only; not a mesh, UV layout, texture map or Unity-ready prop. |
| `Assets/Story/References/Van_Staff_3D_Side_FINAL.png` | CANONICAL SINGLE-VIEW STAFF MODELING REFERENCE; approved; 1024 × 1536 RGB; SHA-256 `9c01e17f7f12a50d8b47d70ee14060b8d3c83d928569d15ce6441a544eb6285c` | One strict 90-degree side orthographic staff matching the approved front height/margins, with narrow crystal edge, edge-on gold insert, overlapped cradle depth, straight shaft and side-profile ferrule. | Side view only; not a mesh, UV layout, texture map or Unity-ready prop. |
| `Assets/Story/References/Van_Staff_3D_Back_FINAL.png` | CANONICAL SINGLE-VIEW STAFF MODELING REFERENCE; approved; 1024 × 1536 RGB; SHA-256 `cfff203df53d955dad8da2d153471aaf850445153a3c2a3a63178ed51c61728c` | One strict rear orthographic staff matching the approved front/side height, margins and baseline, using the rear cyan-crystal facets, rear gold insert/cradle, long shaft and back ferrule surfaces. | Back view only; not a mesh, UV layout, texture map or Unity-ready prop. |

### Reference Conflict Resolution

- The Global Visual Rules PDF has stale text on its **old Van Aert** page that
  says black, slightly messy hair. That conflicts with the page's own white-
  haired artwork, the approved later-panel continuity, and the established
  older Van Aert identity.
- For **older Van Aert**, use white half-tied hair, tapered white beard,
  glasses, blue eyes, lean aged build, and a navy-and-gold robe. The stale
  black-hair caption is explicitly invalid for all future generation prompts.
- For **Vander Poel**, use loose white hair, broader beard, glasses, teal
  mantle, blue diamond chest gem, and a frail/hunched posture. The finalized
  Vander Poel character sheet is the current appearance authority. The older
  V2 reference remains archival only and must not be used for prompts.
- Van Aert's staff remains one dark-brown wood shaft with engraved gold
  fittings and one symmetric light-cyan diamond crystal. No extra branches,
  gemstones, dark-blue crystal, or metal shaft.
- Library guardian statues use one locked design from approved Panel 34: a
  small complete robed figure with both hands clasped at the chest, rendered in
  dark aged bronze/grey stone, standing on a tall narrow rectangular carved
  pedestal with layered cap and stepped base. On the stone bay left of the
  gateway, the locked horizontal order from right to left is gateway column,
  statue/pedestal, then wall torch; the torch sits higher but separately to the
  statue's left, never directly above it. Do not substitute portrait busts,
  alternate figures, weapons, books, or different pedestals.

## Prologue Panels

### Panel 01 — Peace Before the Great War

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED v2 |
| Canonical asset | `Assets/Story/Approved/Prologue_Panel_01_V2_SOURCE_APPROVED.png` |
| Original source | `C:\Users\David\Downloads\ChatGPT Image Aug 26, 2026, 10_24_55 PM.png` |
| SHA-256 | `6bc7c3cd986d5521b526e9e18611ac8b0a3505bea8a80ddff4cbd8da8b1fc208` |
| Dimensions | 1672 × 941 |
| Color mode | RGB, opaque |
| Aspect ratio | 1.776833:1; visually near 16:9 but needs a separate standardized 1920×1080 final export |
| Story beat | Young Van Aert watches Nevali play with colorful wooden blocks before the Great War. |
| Character continuity | Young Van Aert and child Nevali establish their approved early-era identities. |
| Environment continuity | Warm wizard family home; books, plants, wood furniture, bright arched window, blue-and-gold banner. |
| Emotional direction | Safety, family, warmth, affection, and memory. |
| Explicit exclusions | No wife, extra character, modern object, caption, speech bubble, watermark, or random writing. |
| Version history | `Prologue_Panel_01_SOURCE_APPROVED.png` remains an approved v1 archival source. The former v2 source is archived at `Assets/Story/Work/Panel_01/Prologue_Panel_01_V2_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. The explicitly approved Younger Van Style Candidate V2 now occupies the canonical v2 path. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 was rejected because Van still read as a teenager. Candidate V2 uses the canonical late-30s Younger Van reference and preserves the approved composition, Nevali, home, and staff; it was promoted after explicit user approval. |
| Remaining technical work | Create a separate 1920×1080 final export without altering the approved composition or cropping Van Aert, Nevali, or the warm-home context. |

### Panel 02 — Great War and Broken Language

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED v2 |
| Canonical asset | `Assets/Story/Approved/Prologue_Panel_02_V2_SOURCE_APPROVED.png` |
| Original source | `C:\Users\David\Downloads\ChatGPT Image Aug 26, 2026, 10_25_06 PM.png` |
| SHA-256 | `1dbb9387f33f958859f7e6f76de521e197247c0fc30838edbfd7bd09dda03ecd` |
| Dimensions | 1672 × 941 |
| Color mode | RGB, opaque |
| Aspect ratio | 1.776833:1; visually near 16:9 but needs a separate standardized 1920×1080 final export |
| Story beat | Young Van Aert confronts the devastation of the Great War beneath Broken Language corruption. |
| Character continuity | Young Van Aert uses the canonical black hair, blue eyes, navy-and-gold robe, lean build, and full-height staff. |
| Prop continuity | One canonical staff: dark-brown shaft, engraved gold fittings, one symmetric light cyan-blue crystal. |
| Environment continuity | Collapsed fantasy city, rubble, fire, smoke, ruined banners, and storm clouds. |
| Corruption rule | Broken Language is abstract violet corruption only. It must not become readable text, generic letters, or a defined monster identity. |
| Emotional direction | Determination through exhaustion; the scale of loss must outweigh the hero pose. |
| Explicit exclusions | No additional weapon, duplicate staff, readable glyphs, large beam, modern object, caption, speech bubble, watermark, or unrelated character. |
| Remaining technical work | Create a separate 1920×1080 final export without altering the approved composition or cropping the staff, face, or ruined-city context. |
| Version history | The pre-revision v2 source is archived at `Assets/Story/Work/Panel_02/Prologue_Panel_02_V2_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. The explicitly approved Younger Van Style Candidate V1 now occupies the canonical v2 path. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 uses the canonical late-30s Younger Van identity while preserving the ruined city, abstract violet Broken Language, pose, gaze, and full-height staff; it was promoted after explicit user approval. |

### Panel 03 — Great War Combat and Word Magic

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED |
| Canonical asset | `Assets/Story/Approved/Prologue_Panel_03_SOURCE_APPROVED.png` |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 3.png` |
| SHA-256 | `cb041beb59b1c904b246271a509b795fa6eb0a5cffd9c155de69455f11b42e2f` |
| Dimensions | 1672 × 941 |
| Color mode | RGB, opaque |
| Aspect ratio | 1.776833:1; visually near 16:9 and suitable for the later standardized 1920×1080 export pass |
| Story beat | Young Van Aert fights during the Great War, confronting the Broken Language threat through true Word magic. |
| Character continuity | Young Van Aert uses the canonical black hair, blue eyes, navy-and-gold robe, lean build, and full-height staff. |
| Word-magic exception | Readable floating A-G tiles are intentional manifestations of Van Aert’s true Word magic. They are not Broken Language text. |
| Broken Language rule | The central shadow entity and surrounding violet forms remain abstract corruption; do not add readable corruption glyphs or define a new monster identity. |
| Environment continuity | Ruined burning city, rubble, broken banners, storm clouds, and dark violet corruption above. |
| Emotional direction | Active resistance under overwhelming pressure; Van Aert remains small against the threat but visibly defiant. |
| Explicit exclusions | No extra weapon, duplicate staff, modern object, caption, speech bubble, watermark, or unrelated character. |
| Remaining technical work | Create the standardized 1920×1080 Unity export without altering or cropping the approved 16:9 composition. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 uses the canonical adult rear silhouette and robe construction, preserves the colossal shadow corruption, burning city, respectful fallen defenders, connected staff, and intentional A–G Word-magic tiles, and resolves the former 4:3 format. The pre-revision source is archived at `Assets/Story/Work/Panel_03/Prologue_Panel_03_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. |

### Panel 04 — Return Home

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED v2 |
| Canonical asset | `Assets/Story/Approved/Prologue_Panel_04_V2_SOURCE_APPROVED.png` |
| Original source | `C:\Users\David\Downloads\ChatGPT Image Aug 26, 2026, 10_27_31 PM.png` |
| SHA-256 | `d12bcdcf5fa6c34bd4d2cea9296bd5ca01a94a24699c39f393b2652e5af7438e` |
| Dimensions | 1672 × 941 |
| Color mode | RGB, opaque |
| Aspect ratio | 1.776833:1; visually near 16:9 but needs a separate standardized 1920×1080 final export |
| Story beat | Van Aert returns to the destroyed home, finds his wife, and realizes that Nevali is missing. |
| Character continuity | Young Van Aert remains in his canonical navy-and-gold robe with black hair and no facial hair. |
| Prop continuity | Staff is intentionally off-frame in the approved v2 composition. Child’s blocks and blue hair ribbon remain in-frame as evidence of Nevali’s absence. |
| Environment continuity | Collapsed home, smoke, grey-blue rubble, damaged bookshelves, child’s blocks, blue hair ribbon, and no surviving warm domestic light. |
| Emotional direction | Overwhelming exhausted grief and shock. The framing makes Van Aert small within the destruction. |
| Wife rule | Peaceful and respectful presentation only: no gore, visible wound, blood, or horror treatment. |
| Explicit exclusions | No Nevali, extra character, readable corruption text, caption, speech bubble, watermark, or warm family-memory palette. |
| Version history | `Prologue_Panel_04_SOURCE_APPROVED.png` remains an approved v1 archival source. The pre-revision v2 source is archived at `Assets/Story/Work/Panel_04/Prologue_Panel_04_V2_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`; explicitly approved Style Candidate V2 now occupies the canonical v2 path. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 was rejected because it forced the entire staff inside the frame and made it visibly too short. Candidate V2 preserves the canonical late-30s Younger Van, kneeling grief action, wife, destroyed home, blocks, and ribbon while extending the straight staff beyond the lower-right crop so its standing-height scale is physically credible. |
| Remaining technical work | Create a separate 1920×1080 final export without cropping Van Aert, his wife, the child’s items, or the devastation surrounding them. |

### Panel 05 — Young Van Seeks Refuge at the Library

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V4 promoted on 2026-08-27 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_05_SOURCE_APPROVED.png` |
| Approved SHA-256 | `f53f449719388b47018041fe1e469bbd0391adb7b13a8222939a3226d5be3e9f` |
| Candidate asset | `Assets/Story/Work/Panel_05/Prologue_Panel_05_CANDIDATE_V1.png` |
| Latest generated candidate | `Assets/Story/Work/Panel_05/Prologue_Panel_05_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `c3777a6a79ea83e683e0ed7e92105956c7c283c6d0ccfd3a0e20bfa6ad5ea354` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Current generated candidate | `Assets/Story/Work/Panel_05/Prologue_Panel_05_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `8b87e907402080f357dd13ca4828093c58ac82c379f00a0a203301d0065326a4` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Approval candidate | `Assets/Story/Work/Panel_05/Prologue_Panel_05_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `2e206613cb11dfe588f80c46baf929550336e81f8e0aa34129a3058937c9eb06` |
| Candidate V4 dimensions | 1671 × 941, near-16:9 landscape; standardized Unity export remains pending |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 5.png` |
| SHA-256 | `578dd2cc68cea18a92051e3b654f0b4e9eefd376284f9470bdb04bf90986e487` |
| Dimensions | 1086 × 1448, portrait |
| Story placement | Immediately after the Great War and return-home loss, before the library conversation. Young Van seeks refuge with Vander Poel. |
| Candidate V2 status | REJECTED: it incorrectly depicts Older Van and drifts from the approved Panels 1 and 11 visual treatment. Preserve it only as rejected iteration evidence. |
| Required character | Young Van Aert only: black slightly messy hair, blue eyes, lean build, no beard, canonical navy-and-gold robe, and circular chest fastening. |
| Panel 5 primary style reference | `Assets/Story/Approved/Prologue_Panel_01_V2_SOURCE_APPROVED.png`; match its clean outlined fantasy-comic treatment and readable color separation. |
| Approved environment | Canonical front study chamber viewed from inside toward the lower-right exterior door at approximately 3 PM, with the main table/rug, shelves, central Gothic gateway, and warm sunlight preserved. |
| Emotional direction | Immediate post-war exhaustion, grief, displacement, and the first movement toward refuge; do not make the scene triumphant. |
| Staff direction | Van may carry the canonical staff naturally; its placement and lighting must remain physically believable. |
| Prohibited | Older Van Aert, generic portal fantasy, premature ancient-book reveal, Vander Poel appearing before the next approved beat, portrait final format, caption, speech bubble, watermark, or random writing. |
| Candidate V2 audit | Rejected by explicit user decision on 2026-08-27 because the character age and art-style continuity are wrong. |
| Candidate V3 audit | Pending user approval. Correct Young Van identity, single canonical staff, post-war exterior, lower-right entry framing, readable study chamber, one central Gothic gateway, and substantially closer Panel 1 V2 style. |
| Candidate V4 audit | SOURCE APPROVED. Preserves V3 while replacing the ruined/night exterior with a quiet 3 PM afternoon approach and physically motivated warm sunlight across the doorway, Van, staff, and floor. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 uses the canonical late-30s Younger Van while preserving the lower-right entry composition, centered gateway, library geometry, 3 PM doorway light, and single crown-height-to-floor staff. The pre-revision source is archived at `Assets/Story/Work/Panel_05/Prologue_Panel_05_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva text and page placement remain pending. |

### Panel Numbering Notice

- The former Panel 5-11 continuity numbering is superseded from Panel 5 onward
  by explicit user correction on 2026-08-26.
- Panel 5 is now Young Van Aert seeking refuge at the library immediately after the Great War and return-home loss.
- Do not infer Panel 6 onward from the older numbering. Each subsequent panel
  must be submitted and locked in sequence.

### Panel 06 — Young Van Tells Vander Poel About the Loss

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V3 promoted on 2026-08-27 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_06_SOURCE_APPROVED.png` |
| Approved SHA-256 | `87aaaf13168bb01a524e24a1d6799ad67bfb571318019858e5b6bac41df6b13d` |
| Candidate asset | `Assets/Story/Work/Panel_06/Prologue_Panel_06_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_06/Prologue_Panel_06_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `e269b3a3eb2ff8b9ab5cfb2f7799d97816f1dfdf0023ec418a7d7e866c63188e` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Approval candidate | `Assets/Story/Work/Panel_06/Prologue_Panel_06_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `094df9a3ae71614ae81fe63548feef32c43c6a4a06c2ca21cb19f1015384f0da` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 6.png` |
| SHA-256 | `7a1faf5cb88e4f889df54a1579d68d4bd2dd0fdb0791e46ad3b50426452a67d1` |
| Dimensions | 1448 × 1086, 4:3 |
| Story placement | Immediately after approved Panel 5: Young Van has entered Vander Poel’s library seeking refuge after the Great War. |
| Locked event | Young Van tells Vander Poel that his wife died and that Nevali is missing. |
| Speaker/listener | Van Aert speaks; Vander Poel listens. |
| Required correction | Van must use the young-era identity: black slightly messy hair, blue eyes, clean-shaven face, lean build, navy-and-gold robe, and circular chest fastening. The old candidate's white-haired Van is rejected. |
| Vander requirement | Vander Poel must retain the teal mantle, blue diamond chest gem, loose white hair, broader beard, and distinct frail posture. |
| Required format | Landscape 16:9 Unity final, with sufficient caption-safe space. |
| Emotional direction | Van is exhausted, grieving, and restrained rather than theatrical; Vander responds with quiet attention, concern, and controlled authority. |
| Pending decision | Exact final dialogue wording. |
| Primary style reference | Approved Panel 1 V2; preserve its clean bold outlines, readable shapes, controlled colors, and Young Van identity. |
| Do not inherit | The rejected candidate's incorrect character identities, generic library, art treatment, or 4:3 aspect ratio. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-27. Young Van uses the intended cel-shaded style, but Vander and the environment use more painterly gradients, realistic facial/beard rendering, and denser surface detail, making the frame look assembled from different art styles. |
| Required V3 correction | Preserve the V2 story composition and identities while applying Panel 1 V2's clean outline weight, simplified readable shapes, and controlled cel shading consistently to Vander and the entire environment. |
| Candidate V3 audit | SOURCE APPROVED. Whole frame follows one cel-shaded production language: Young Van, Vander, beard/hair, robes, furniture, stonework, shelves, and lighting share consistent outlines, simplified shapes, and controlled value blocks. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 uses both canonical character references, preserving Van-left/Vander-right blocking, reciprocal eye contact, Van's speaking gesture, Vander's clasped listening posture, centered gateway, open book, and single cropped Van staff at far left. The pre-revision source is archived at `Assets/Story/Work/Panel_06/Prologue_Panel_06_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva dialogue and page placement remain pending. |

### Panel 07 — Library Grief Moment

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V2 promoted on 2026-08-27 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_07_SOURCE_APPROVED.png` |
| Approved SHA-256 | `0cd84dd381f9e5cd264e520de69f720cc4b13cc928916f981212293a651b595b` |
| Candidate asset | `Assets/Story/Work/Panel_07/Prologue_Panel_07_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_07/Prologue_Panel_07_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `88e300a449c55474ebc0ac68cfd9caf03cee59bfc099881d6e92810c96d57638` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 7.png` |
| SHA-256 | `25a3ecd369de1988bcba34316804866cdf9a8aae76cdbd21c7fea605f6e2a933` |
| Dimensions | 1448 × 1086, 4:3 |
| Story placement | Immediately after approved Panel 6. Young Van sits at the study table after confessing the loss of his wife and Nevali's disappearance. |
| Locked story action | Young Van bows his head and holds one hand against his chest in silent restrained grief. Vander is outside the frame. |
| Pose/composition authority | The rejected V1 preserves the intended Young Van pose, table placement, close-medium emotional framing, and empty space on the right; these may be reused. |
| Rejection reason | The V1 library environment is not the canonical library and the full frame is not guaranteed to follow the locked Panel 1 V2 cel-shaded production language. |
| Environment direction | Place the same action at the canonical main study table, retaining only landmarks physically visible from the chosen close-medium camera. |
| Art direction | Panel 1 V2 controls the entire frame: clean bold outlines, simplified readable shapes, controlled cel shading, and disciplined navy/teal/gold/amber color separation. |
| Required character | Young Van: black slightly messy hair, blue eyes, clean-shaven face, lean build, navy-and-gold robe, circular fastening. |
| Pending decision | Final Canva narration or dialogue, if any. |
| Prohibited | Vander or another character in frame, non-canonical library, mismatched painterly environment, Older Van, 4:3 final format, caption, speech bubble, watermark, or random writing. |
| Candidate V2 audit | SOURCE APPROVED. Preserves the locked bowed-head/hand-to-chest grief pose, uses Young Van continuity, replaces the generic library with the canonical study table/shelves/gateway, keeps the full frame cel-shaded, and leaves wide Canva text space on the right. |
| Younger Van style revision | SOURCE APPROVED. Candidate V1 was rejected because its stone wall bay left of the gateway was compressed relative to approved Panel 34. Candidate V2 was off-target; Candidate V3 fixed wall width; Candidate V4 locked the statue design but incorrectly stacked the torch above it. Candidate V5 preserves the wide wall and locked statue design while matching Panel 34's horizontal order from right to left: gateway column, statue/pedestal, then wall torch. The pre-revision source is archived at `Assets/Story/Work/Panel_07/Prologue_Panel_07_PRE_YOUNG_VAN_REVISION_ARCHIVE.png`. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva narration and page placement remain pending. |

### Panel 08 — Vander Poel Centered in the Library

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V4 promoted on 2026-08-27 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_08_SOURCE_APPROVED.png` |
| Approved SHA-256 | `9eee261388b9483c614c9cb39cbe234646d9e0dd5aa66cc4fa8f4929d4729da4` |
| Candidate asset | `Assets/Story/Work/Panel_08/Prologue_Panel_08_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_08/Prologue_Panel_08_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `4314f7ad681f795b46cbc1ead45d4a99444eed75bccb6376fd1de235f2b4940d` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Approval candidate | `Assets/Story/Work/Panel_08/Prologue_Panel_08_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `25149f9cbfdf683ee0375371d0ed8a04e48e19a495c89cbed714c3b9d18ce45d` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Final-edit candidate | `Assets/Story/Work/Panel_08/Prologue_Panel_08_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `9eee261388b9483c614c9cb39cbe234646d9e0dd5aa66cc4fa8f4929d4729da4` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 8.png` |
| SHA-256 | `037f49f8ba5ef1f98b30c8dd485cd5c1d0989bfcf8ee746376d0d7cde7fa0fa1` |
| Dimensions | 1448 × 1086, 4:3 |
| Story placement | Immediately after approved Panel 7. Vander silently considers Young Van's confession. |
| Locked story action | Vander Poel is centered at the main study table in a contemplative pose, one hand near his chin, quietly processing the loss. Young Van is outside the frame. |
| Pose/composition authority | The rejected V1 controls only the centered contemplative pose and medium portrait framing. |
| Rejection reason | The V1 library environment, character rendering, staff ownership, and whole-frame art style are not canonical. |
| Required character | Vander Poel only: loose white-grey hair, broad white beard, glasses, blue eyes, teal mantle with gold trim, blue diamond chest gem, frail scholarly posture. |
| Environment direction | Canonical main study table and front chamber, preserving only landmarks physically visible from the medium camera. |
| Art direction | Panel 1 V2's clean outlined cel-shaded language applies to Vander and the entire environment. |
| Staff rule | No staff in frame. Vander does not own or hold Van's staff. |
| Pending decision | Final Canva narration or dialogue, if any. |
| Required format | Landscape 16:9 Unity final. |
| Prohibited | Young Van or another character in frame, staff, navy-robed Vander, non-canonical library, painterly realism, caption, speech bubble, watermark, random writing, or 4:3 final format. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-27. The centered Vander pose and cel shading are strong, but the background shelves use diagonal corner-perspective convergence instead of the canonical wall-aligned shelf presentation. |
| Required V3 correction | Use a straight-on frontal camera toward one canonical shelf wall so every shelf upright is vertical and every shelf board is horizontal; preserve centered Vander, table, cel shading, and identity. |
| Candidate V3 audit | Pending user approval. Straight-on wall-aligned bookshelf grid with vertical uprights and horizontal boards; centered canonical Vander, no staff or extra character, horizontal table base, blank book, and consistent whole-frame cel shading. |
| Candidate V4 audit | SOURCE APPROVED. Removes only the two stacked closed books from the lower-right table area and leaves a clean reconstructed tabletop; the central open book remains. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva dialogue and page placement remain pending. |

### Panel 09 — Vander Poel on the Right Side

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V15 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_09_SOURCE_APPROVED.png` |
| Approved SHA-256 | `5fa9d9fa36b8bd306cfbf0788663d1396fc91cfd1dbc3a33933a9b6baf63bc8a` |
| Candidate asset | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `0d3586d1069dfbc35b2b92c1d8bdcfdddd1d5c7b711de5bd7e0c9eb4f31f93af` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Approval candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `8444b6fa16146d6f6d0eb2c97dd3ddb56cd8383e67131d52d9c67bdb673fc3a8` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Chronology-corrected candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `9e07e2b92a212d3a707abc8babe2581991d84726d45505b52380c188015a846d` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Camera blocking reference | `Assets/Story/Work/Panel_09/Panel_09_CAMERA_BLOCKING_REFERENCE.png`; SHA-256 `7da1c08c12221b207a1955ffb0427dbaf72554b5e287b30b9bff2c49e55314dc` |
| Expanded camera blocking reference | `Assets/Story/Work/Panel_09/Panel_09_CAMERA_BLOCKING_REFERENCE_V2.png`; SHA-256 `5984894a52e321eee5e813a27420051e9e3b223dd9969d5bf095221aeef12b9e` |
| Canonical generation specification | `docs/story-prompts/PROLOGUE_PANEL_09_GENERATION_PROMPT.txt`; SHA-256 `95aa393395fdb659593fe2fb67f85cb2b99657a3c85d4cba98431e3fd2b97e91` |
| Annotated-camera candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V5.png` |
| Candidate V5 SHA-256 | `6348731804a6e8a9b8eae991520b4a398cde39d27dff06f9be24a9d2f6b73299` |
| Candidate V5 dimensions | 1672 × 941, 16:9 landscape |
| Strict-architecture candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V6.png` |
| Candidate V6 SHA-256 | `80bff3a44b4755d5ed27814129ed10d7f26b66d9dbfc8f1733a5bc68e37b3827` |
| Candidate V6 dimensions | 1672 × 941, 16:9 landscape |
| User-specification candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V7.png` |
| Candidate V7 SHA-256 | `44689c8fc733becbb3c125e97f77603da750f79c12cab8471505577ee3afc432` |
| Candidate V7 dimensions | 1672 × 941, 16:9 landscape |
| Panel-5-environment test candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V8.png` |
| Candidate V8 SHA-256 | `ce1f2fe62fd30705342c29722b68f0aacc45828c8207674fff42c7223d50d49c` |
| Candidate V8 dimensions | 1672 × 941, 16:9 landscape |
| Final-cleanup candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V9.png` |
| Candidate V9 SHA-256 | `7ad59c25f229212d3e35b0a2be4a3cf15bf576d7e80f7f6ea3ff89252b10239b` |
| Candidate V9 dimensions | 1672 × 941, 16:9 landscape |
| Anatomy-corrected candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V10.png` |
| Candidate V10 SHA-256 | `0edf5a8b54eb77b89fcb1dd102bbbdc315c28dd61c70eb990a88731f31c076f0` |
| Candidate V10 dimensions | 1672 × 941, 16:9 landscape |
| Book-removed approval candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V11.png` |
| Candidate V11 SHA-256 | `c7db944815b4b45f29c11e8ab541fde83090385b4bc5ee80020cc58205c1d618` |
| Candidate V11 dimensions | 1672 × 941, 16:9 landscape |
| Right-bay geometry candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V12.png` |
| Candidate V12 SHA-256 | `9ea8260cbba9b9ddec264a63ff73d7e2e22884b2640f1cb007d0ade06ec1603d` |
| Candidate V12 dimensions | 1672 × 941, 16:9 landscape |
| Geometry-cleanup approval candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V13.png` |
| Candidate V13 SHA-256 | `5c0989e22532fa9e6efe30f8a1b6de95091e69247de2aa8ef39cf2924fb1969c` |
| Candidate V13 dimensions | 1672 × 941, 16:9 landscape |
| Canonical-shelving approval candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V14.png` |
| Candidate V14 SHA-256 | `3e30bdb4c566604c0337b231fe9bd984b331ddef85eaf26d16266e0d8de2a417` |
| Candidate V14 dimensions | 1672 × 941, 16:9 landscape |
| Focus/style-restored approval candidate | `Assets/Story/Work/Panel_09/Prologue_Panel_09_CANDIDATE_V15.png` |
| Candidate V15 SHA-256 | `5fa9d9fa36b8bd306cfbf0788663d1396fc91cfd1dbc3a33933a9b6baf63bc8a` |
| Candidate V15 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 9.png` |
| SHA-256 | `87c38652898a15a9253c9b5e995100b616abd9ed6f87d80eef0fa80b22d5d845` |
| Dimensions | 1411 × 1114, 1.266607:1 |
| Story placement | A deliberate time jump after Panel 8: decades have passed inside Vander Poel's library. |
| Locked story action | Older Van sits silently at the main table, thinking about Nevali and a recent dream he has not yet disclosed. Vander has just emerged from the cool rear archive through the central Gothic gateway and pauses after noticing Van's distress. Neither character speaks. |
| Pose/composition authority | The rejected V1 controls the two-level blocking and emotional action: Older Van seated left/foreground with head supported by one hand; Vander standing right/background. |
| Rejection reason | V1 captures the intended decades-later beat but uses the wrong library environment and mixed/non-canonical rendering. |
| Older Van requirement | White half-tied hair, tapered white beard, glasses, blue eyes, lean aged build, navy-and-gold robe, circular fastening; seated in quiet long-term grief. No blue diamond chest gem. |
| Vander requirement | Loose white-grey hair, broader white beard, glasses, blue eyes, teal mantle with gold trim, blue diamond chest gem; standing on the right with frail concerned posture. |
| Prop rule | Van's canonical staff must rest naturally beside his chair as a secondary prop. Vander has no cane or staff. Any nearby book is ordinary, inactive, non-glowing, and unreadable. |
| Environment direction | Same canonical front study chamber and table. Use straight wall-aligned shelves where visible; no slanted shelf grid. |
| Art direction | Panel 1 V2's cel-shaded language applies consistently to both characters and the full environment. |
| Pending decision | Exact Canva time-jump narration and whether Vander speaks in this panel. |
| Required format | Landscape 16:9 Unity final. |
| Prohibited | Young Van, two visually identical elderly clones, blue gem on Van, navy-robed Vander, staff/cane, non-canonical library, slanted shelves, painterly realism, caption, speech bubble, watermark, random writing, or a non-16:9 final. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-27. It incorrectly continues with Young Van instead of showing the decades-later Older Van; it also introduces unapproved window/plant/flower environment drift. |
| Required V3 correction | Reuse approved Panel 8's exact straight shelf wall, central banner, paired sconces, lower cabinets, clean tabletop, central open book, and lighting. Add only the locked Young Van-left/Vander-right character action; introduce no environment object. |
| Candidate V3 audit | REJECTED by explicit user decision on 2026-08-27. It correctly reuses Panel 8's environment but still depicts Young Van and immediate response blocking instead of the locked decades-later Older Van scene. |
| Required V4 correction | Preserve V1's Older Van-left/Vander-right time-jump blocking, but rebuild both identities and the entire frame in Panel 1 V2's cel-shaded style against approved Panel 8's exact shelf-wall environment. |
| Candidate V4 audit | REJECTED by explicit user decision on 2026-08-28. Chronology, identities, strict environment reuse, and cel shading are correct, but the camera/blocking does not match the newly supplied annotated table-side shot. |
| Required V5 correction | Use the annotated camera position on the right side of the canonical main table, looking diagonally across it. Place Older Van at the marked near/bottom table position and Vander at the marked left-side position while retaining V4 identities, emotional action, cel style, and canonical environment. |
| Candidate V5 audit | Pending user approval. Matches the annotated right-side camera: Older Van dominates the right/near foreground and Vander stands farther left. Visible gateway arches, rear shelves/globe, right-side door/banner/window, chandelier, table, rug, lantern, and blank open book correspond to the canonical map; no staff, cane, plants, or flowers. |
| Candidate V6 audit | Pending user approval. Preserves V5's annotated camera and identities while correcting the background to one central blue-trim Gothic gateway; removes the invented row of additional open arches and retains canonical shelves, right-side door/banner/window, chandelier, table/rug, lantern, and blank open book. |
| Candidate V6 disposition | Superseded before approval by the user's expanded Panel 9 specification: Vander must visibly arrive through the gateway, neither character speaks, and Van's staff must be present beside his chair. |
| Required V7 correction | Follow the expanded annotated camera and saved generation specification exactly, with warm study versus cool archive lighting, Vander at/just beyond the gateway, silent Older Van near camera, and canonical staff beside Van. |
| Candidate V7 audit | Pending user approval. Matches expanded right-side camera blocking; silent Older Van is nearest at the table with canonical staff beside his chair; silent Vander emerges through the single gateway with cool archive edge light; canonical table/rug, red runner, shelves, globe, chandelier, right-side door/banner/window, candelabra, and inactive unreadable book remain spatially coherent. |
| Candidate V8 audit | TEST ONLY; not approval-ready. Strong locked composition: Older Van left foreground, Vander center-right emerging from the gateway, shelves framing both sides, single gateway/archive depth, staff beside Van. Environment freeze failed at the table: the approved two-book arrangement was reconstructed/relocated and the visible open book contains pseudo-writing. |
| Candidate V9 audit | Pending final user approval. Removes the extra book and places one ordinary open book directly in front of Van; removes the far-right bookshelf and reconstructs a stone wall while retaining the globe/table; keeps Van/staff/book sharp and applies restrained depth-of-field softness to Vander and the background. |
| Candidate V10 audit | Intermediate anatomy correction: reconstructs Van's resting hand as a separate five-finger elderly hand on the tabletop while otherwise preserving V9; superseded before approval by the instruction to remove the book entirely. |
| Candidate V11 audit | Pending final user approval. Removes the book completely, restores uninterrupted tabletop, preserves Van's corrected five-finger resting hand and all V9 environment/character/blur decisions. |
| Candidate V12 audit | GEOMETRY FIX SUCCEEDED BUT NOT APPROVAL-READY. Restores a broad right study bay and natural spacing around the gateway pillar, sconce, statue, globe table, chair, wall, and floor. Preserves no-book table, corrected hand, characters, staff, and blur. Regression: reintroduces a far-right bookshelf that V11 explicitly removed. |
| Candidate V13 audit | Pending final user approval. Preserves V12's restored broad right study bay and natural spacing while removing the reintroduced far-right bookshelf and replacing only that strip with matching stone wall. Retains no-book table, corrected hand, Van-focused depth of field, characters, staff, gateway, globe table, statues, sconces, and lighting. |
| Candidate V14 audit | Pending final user approval. Restores a complete canonical right outer-wall bookshelf bay matching the left shelf's dark carved wood, frame thickness, horizontal spacing, cabinet base, book scale/palette, outlines, and cel shading while preserving V13's broad room geometry, globe spacing, characters, no-book table, corrected hand, focus blur, and all other elements. |
| Candidate V15 audit | SOURCE APPROVED. Preserves V14's broad room geometry and matching canonical outer-wall shelves while restoring Panel 1-style clean cel shading and V11-style focus hierarchy: Older Van, staff, hands, robe, and near table are sharp; Vander, gateway, shelves, stonework, statues, globe area, and distant environment receive gradual optical softness. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva time-jump narration and comic-page placement remain pending. |

### Panel 10 — Van Aert Tells Vander Poel About the Dream

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED; V2 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_10_SOURCE_APPROVED.png` |
| Approved SHA-256 | `c683db0f48ef01f3659385f5b29cc76bfe4cce1e237a182e5f962736212ffb2d` |
| Candidate asset | `Assets/Story/Work/Panel_10/Prologue_Panel_10_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_10/Prologue_Panel_10_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `c683db0f48ef01f3659385f5b29cc76bfe4cce1e237a182e5f962736212ffb2d` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 10.png` |
| SHA-256 | `e033bc2bd4f94da1d2bdfd9dd3cf53700a6b63026802db3b990291b7c9ac9f09` |
| Dimensions | 1254 × 1254, square |
| Locked event | Van Aert tells Vander Poel about the dream. |
| Speaker/listener | Van Aert speaks; Vander Poel listens. |
| Rejection reason | The art style and the square ratio must change. The current image is not a visual reference authority. |
| Story placement | Immediately after approved Panel 9. Older Van finally explains that he recently dreamed about Nevali. |
| Dream rule | The dream is conveyed only through Van's acting and later Canva dialogue. Do not show Nevali, a vision, flashback, projection, glowing book, or magical effect. |
| Older Van requirement | Left/foreground speaker: white half-tied hair, tapered beard, glasses, navy-and-gold robe, circular fastening, no blue gem; restrained speaking gesture. |
| Vander requirement | Right/midground listener: loose white-grey hair, fuller beard, glasses, teal-and-gold mantle, blue diamond chest gem; mouth closed and attentive. |
| Camera/composition | Landscape medium-close two-shot derived from the rejected V1's conversational intimacy. Van remains the primary focus; Vander may be slightly softer but clearly readable. |
| Previous-panel background audit | **Reuse Exact** from approved Panel 10. Vander remains at the same table position with the central gateway/cool archive directly behind him; a tighter crop may exclude Van but cannot replace the gateway with a shelf wall. |
| Environment authority | Approved Panel 10 is the exact location/camera/background authority; approved Panel 9 remains broader room context only. |
| Table/prop rule | Preserve Panel 9's empty no-book main table. Van's staff may remain outside the crop or as a secondary edge element; never duplicate it or give it to Vander. |
| Required format | Landscape 16:9 Unity final with caption-safe space. |
| Pending decision | Exact final Canva dialogue. |
| Prohibited | Dream imagery, Nevali, book/message, new library object, changed environment geometry, mixed/painterly style, square final format, caption, speech bubble, watermark, or random writing. |
| Candidate V2 audit | SOURCE APPROVED. Older Van speaks left with restrained open-hand gesture; Vander listens right with closed mouth; navy/circular-fastening versus teal/blue-gem identities remain distinct; no book/dream/Nevali/text; tighter two-shot retains gateway, shelves, masonry, sconces/statues, empty table, and consistent whole-frame cel shading. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting or cropping the approved source. Canva dialogue and comic-page placement remain pending. |

### Panel 11 — Nevali’s Magical Appearance

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED |
| Canonical asset | `Assets/Story/Approved/Prologue_Panel_11_SOURCE_APPROVED.png` |
| Original source | `C:\Users\David\Downloads\ChatGPT Image Aug 26, 2026, 10_27_41 PM.png` |
| SHA-256 | `fa6b4f3e51d78f578583a57acf1996e79476d7c7bb322f76874a404600df8718` |
| Dimensions | 1672 × 941 |
| Color mode | RGB, opaque |
| Aspect ratio | 1.776833:1; visually near 16:9 but needs a separate standardized 1920×1080 final export |
| Approved visual beat | Nevali appears in a warm magical manifestation before an older wizard in a library-like study; the image establishes a tender, hopeful contact moment. |
| Character continuity | Nevali retains brown braided hair, blue bow, blue eyes, and cream/blue/gold dress. The older wizard uses white tied-back hair, glasses, and a navy-and-gold robe. |
| Approval boundary | This source is approved. Its exact sequence placement, speaker identity, dialogue/caption, and relationship to the new library redesign remain to be explicitly locked before Unity sequencing. |
| Sequence placement | Locked immediately after approved Panel 10 as Nevali's visual manifestation/confirmation following Van's dream disclosure. |
| Remaining technical work | Create a separate 1920×1080 final export without overwriting the approved source; reserve a caption-safe area in the Unity composition. |
| Explicit exclusions | No embedded dialogue, speech bubble, watermark, random writing, or unapproved changes to Nevali’s identity. |

### Panel 12 — Vander Poel Portrait Candidate

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V4 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_12_SOURCE_APPROVED.png` |
| Approved SHA-256 | `d87b4ced8e48f4b75d9ca9c7dd3aea23e1e7bdb34f383635260e9cd7f92a3cb3` |
| Candidate asset | `Assets/Story/Work/Panel_12/Prologue_Panel_12_CANDIDATE_V1.png` |
| Current generated candidate | `Assets/Story/Work/Panel_12/Prologue_Panel_12_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `411dc9e11f8444a77f008f11e4ff4a9ce4f843c6f4cf07b2ca80c0e9fd236745` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Focus-corrected approval candidate | `Assets/Story/Work/Panel_12/Prologue_Panel_12_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `b5281aa932525df7f3892b024e2f0b00c1366a95bb1d5dfd8641c07ccd80c696` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Previous-panel-continuity approval candidate | `Assets/Story/Work/Panel_12/Prologue_Panel_12_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `d87b4ced8e48f4b75d9ca9c7dd3aea23e1e7bdb34f383635260e9cd7f92a3cb3` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 12.png` |
| SHA-256 | `34c067a1fd4b0bbebfc281fe692c5f349cb336f87da13641d6223b87e3ea65b9` |
| Dimensions | 1402 × 1122, 1.249554:1 |
| Rejection reason | Vander Poel’s appearance and the library environment both require redesign. |
| Story placement | Immediately after approved Panel 11. Vander silently processes what has just been revealed. |
| Locked story action | Vander alone in frame at the canonical study table, centered in a quiet contemplative reaction; mouth closed, one hand near chin/beard, controlled concern rather than shock. |
| Character correction | Do not use this candidate as Vander Poel’s identity reference. Rebuild him to the approved visual guide: loose white hair, broader beard, glasses, teal mantle, blue diamond chest gem, and a distinct frail/hunched posture. |
| Environment correction | Use the approved canonical straight shelf-wall study-table environment; no generic library reconstruction and no new object. |
| Prop continuity | If the Panel 11 book edge remains visible, it must be the same ordinary open book, non-glowing and unreadable. Do not add stacked books or a new book. |
| Art/focus direction | Panel 1 V2 cel shading applies to Vander and the full frame. Vander is sharp; background shelves/room may be moderately softened. |
| Pending decision | Final Canva narration/dialogue, if any. |
| Required format | Landscape 16:9 Unity final after the character and environment direction are locked. |
| Candidate V2 audit | Intermediate only. Correct centered canonical Vander, straight shelf-wall environment, banner/sconces/cabinets, no extra character/prop, and whole-frame cel shading; background remains too sharp for the intended focal hierarchy. |
| Candidate V3 audit | REJECTED by explicit user decision on 2026-08-28. Vander's identity, pose, cel shading, and focus are correct, but the shelf-wall background breaks immediate continuity: approved Panel 10 establishes the central gateway directly behind Vander. |
| Required V4 correction | Reuse approved Panel 10's exact Vander-side background and camera family, crop tighter around Vander, keep the gateway/cool archive behind him, and preserve the contemplative pose with no Van, book, magic, or new environment. |
| Candidate V4 audit | SOURCE APPROVED by explicit user decision on 2026-08-28. Reuses approved Panel 10's right-side camera family and gateway/archive background; tighter crop excludes Van without inventing another wall; Vander remains sharp in a contemplative hand-at-chin pose while gateway, archive shelves, statues, sconces, and masonry are moderately softened; no book, staff, magic, or new object. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting the approved source, then place final copy and page composition in Canva. |

### Panel 13 — Vander Poel Portrait Candidate

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_13_SOURCE_APPROVED.png` |
| Approved SHA-256 | `bae303fbb2a39c553bc69747a589dd5b341d2dd2a696c2265f23a47cb5ba9400` |
| Candidate asset | `Assets/Story/Work/Panel_13/Prologue_Panel_13_CANDIDATE_V1.png` |
| Current approval candidate | `Assets/Story/Work/Panel_13/Prologue_Panel_13_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `bae303fbb2a39c553bc69747a589dd5b341d2dd2a696c2265f23a47cb5ba9400` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 13.png` |
| SHA-256 | `cb5caac0ab5f90cc0ba722980c32695dd53a6b30f4f897adbf891b57d93ca55a` |
| Dimensions | 1403 × 1121, 1.251561:1 |
| Rejection reason | Same correction as Panel 12: Vander Poel’s appearance and the library environment both require redesign. |
| Story placement | Immediately after approved Panel 12 as a tighter continuation of Vander's silent contemplation. |
| Previous-panel background audit | `Reuse Exact` from approved Panel 12. Retain the central blue-trim Gothic gateway and cool archive passage behind Vander; a tighter crop is allowed, but a shelf-wall substitution is not. |
| Locked focus direction | Vander is the sole sharp subject. Apply restrained optical depth-of-field blur to the gateway and archive background while keeping the architecture recognizable and geometrically unchanged. |
| Character correction | Do not use this candidate as Vander Poel’s identity reference. Use the approved visual-guide requirements: loose white hair, broader beard, glasses, teal mantle, blue diamond chest gem, and a distinct frail/hunched posture. |
| Environment correction | Do not reuse the generic candle-and-shelves treatment. Use approved Panel 12's exact gateway-side environment and camera family without introducing new architecture, furniture, props, or lighting sources. |
| Story status | Visual objective locked as a tighter contemplative continuation; final Canva dialogue/caption remains pending. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision on 2026-08-28. Tighter solo Vander continuation; approved Panel 12 gateway/archive background retained; Vander is sharp while the recognizable background receives restrained optical depth-of-field; no Van, book, staff, magic, text, new prop, or shelf-wall substitution. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting the approved source, then place final copy and page composition in Canva. |
| Required format | Landscape 16:9 Unity final after the character and environment direction are locked. |

### Panel 14 — Two Wizards at the Library Table

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_14_SOURCE_APPROVED.png` |
| Approved SHA-256 | `4a193c8b3391bd747919d230d5cabe6735809fc27ee12b9f2650677b4b27fa01` |
| Candidate asset | `Assets/Story/Work/Panel_14/Prologue_Panel_14_CANDIDATE_V1.png` |
| Current approval candidate | `Assets/Story/Work/Panel_14/Prologue_Panel_14_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `4a193c8b3391bd747919d230d5cabe6735809fc27ee12b9f2650677b4b27fa01` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 14.png` |
| SHA-256 | `18681f7a2ad8cf889e424889c33670fdc8a9e491925d9d80e99a62339eeb4f5d` |
| Dimensions | 1672 × 941 |
| Rejection reason | The art style has changed. Rebuild using the approved Panel 11 art style, which is the current authority for later-panel visual treatment. |
| Story placement | Immediately after approved Panel 13. Vander has finished contemplating and now responds to Older Van at the same study table. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Return from Panel 13's solo close-up to approved Panel 10's established two-shot camera; Panel 10 is the exact background, table, seating, and gateway authority. |
| Locked story action | Older Van listens on frame-left while Vander on frame-right answers with one restrained open-palm explanatory gesture. Both remain seated; the emotional register is serious and controlled. |
| Character authority | Older Van follows approved Panel 10. Vander follows approved Panels 12–13 and the final Vander character sheet. |
| Environment status | Reuse approved Panel 10's exact study-table view and gateway/archive background. Do not use the rejected candidate's grand hall, huge window, banners, chandelier, extra shelves, books, or other props. |
| Focus direction | This is a two-character dialogue shot, so both faces and Vander's speaking hand remain sharp. Use only mild background depth-of-field on the gateway/archive, not solo-shot blur on either character. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision on 2026-08-28. Returns to approved Panel 10's exact same-table two-shot and gateway/archive geometry; Vander speaks with a restrained palm-up gesture while Older Van listens; both characters remain sharp; no book, staff, magic, text, new prop, or rejected grand-hall element. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting the approved source, then place final copy and page composition in Canva. |
| Required format | Landscape 16:9 Unity final, with caption-safe space. |
| Prohibited | Reuse of the current art treatment as authority, caption, speech bubble, watermark, random writing, or treating this candidate as final. |

### Panel 15 — Library Conversation Candidate

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V3 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_15_SOURCE_APPROVED.png` |
| Approved SHA-256 | `02d93e532afefe6402665abec18810b285ebd16aa73a8cb87470ae943a333463` |
| Candidate asset | `Assets/Story/Work/Panel_15/Prologue_Panel_15_CANDIDATE_V1.png` |
| Current approval candidate | `Assets/Story/Work/Panel_15/Prologue_Panel_15_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `9165fd07a594349cee489b7cddbf6471d11d589e498e55734d28ae825c22bc56` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Staff-continuity approval candidate | `Assets/Story/Work/Panel_15/Prologue_Panel_15_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `02d93e532afefe6402665abec18810b285ebd16aa73a8cb87470ae943a333463` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 15.png` |
| SHA-256 | `90ff1117ccd0b017ab7dec66e448b5c234c56ce72aa93a59bff475e9b25e53be` |
| Dimensions | 1672 × 941 |
| Rejection reason | The art style has changed. Rebuild using the approved Panel 11 art style, which is the current authority for later-panel visual treatment. |
| Story placement | Immediately after approved Panel 14 and immediately before the planned walking transition in Panel 16. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Widen approved Panel 14 only enough to show Vander rising and turning toward the same central gateway. Preserve the same study chamber, table, chairs, gateway, and archive direction. |
| Locked story action | Vander rises from the right-side chair and gestures toward the gateway, inviting Older Van to follow. Older Van remains on frame-left, beginning to rise or turning his attention toward the gateway. This is the decision-to-move bridge, not another seated exposition panel. |
| Character authority | Older Van and Vander follow approved Panel 14 exactly. Vander retains the blue diamond chest gem; Older Van retains the circular gold fastening and no gem. |
| Environment status | Reuse the approved Panel 14/10 study-table environment. Do not use the rejected candidate's altered hall, books, candles on the table, staff, banners, or new props. |
| Focus direction | Both characters and Vander's directing gesture remain sharp. Apply mild optical depth-of-field to the gateway/archive background while keeping the path readable. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-28. The movement beat, characters, empty table, gateway, and environment are correct, but Van's canonical staff is missing even though the widened camera reveals its established resting area beside/behind the left chair. |
| Required V3 correction | Add only Van's canonical staff upright at the far-left edge, leaning beside/behind his chair exactly as established in approved Panel 9: dark-brown wooden shaft, engraved gold fittings, one symmetric light-cyan diamond crystal. Preserve every other pixel-level subject, pose, prop absence, environment element, composition, lighting, focus, and style. |
| Candidate V3 audit | SOURCE APPROVED by explicit user decision on 2026-08-28. Restores exactly one canonical staff at the far-left beside/behind Van's chair, matching approved Panel 9 placement and the staff sheet; Van continues to brace with both hands; characters, empty table, chairs, gateway/archive geometry, lighting, focus, and cel-shaded style remain unchanged. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting the approved source, then place final copy and page composition in Canva. |
| Required format | Landscape 16:9 Unity final, with caption-safe space. |
| Prohibited | Reuse of the current art treatment as authority, caption, speech bubble, watermark, random writing, or treating this candidate as final. |

### Panel 16 — Two Wizards Walking Through the Library

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V3 promoted on 2026-08-28 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_16_SOURCE_APPROVED.png` |
| Approved SHA-256 | `7848a928ced34b9fb3dae8252e8ea9d0a42e6fbb344d85a3698681bad8dd6c39` |
| Candidate asset | `Assets/Story/Work/Panel_16/Prologue_Panel_16_CANDIDATE_V1.png` |
| Current approval candidate | `Assets/Story/Work/Panel_16/Prologue_Panel_16_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `d9b1a40129d17ab613e380625092eb6accc7bd7008bd060f31dab4bbcf534f10` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Environment-continuity approval candidate | `Assets/Story/Work/Panel_16/Prologue_Panel_16_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `7848a928ced34b9fb3dae8252e8ea9d0a42e6fbb344d85a3698681bad8dd6c39` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Original source | `C:\Users\David\OneDrive\Desktop\STORYBOARD\STORYBOARD PANEL 16.png` |
| SHA-256 | `1004f4a6036379ac5ab7e87af1ecf4d2ae1ba91787aaea9645b4a9ba252383a7` |
| Dimensions | 1672 × 941 |
| Rejection reason | The art style has changed. Rebuild using the approved Panel 11 art style, which is the current authority for later-panel visual treatment. |
| Story placement | Immediately after approved Panel 15. Vander now leads Older Van through the central gateway into the rear archive. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Reverse to a rear tracking/wide shot from the same front study chamber, looking through the exact approved gateway into the canonical archive. The camera change is required to show movement and destination. |
| Locked story action | Vander walks ahead through the gateway; Older Van follows a short distance behind while carrying his canonical staff. Both are seen mostly from behind. No dialogue acting is required. |
| Character authority | Preserve approved Panel 15 identities and robes. Older Van carries exactly one canonical staff; Vander carries no staff and retains the blue diamond gem only if visible from the angle. |
| Environment status | Use the canonical gateway, red runner, and rear archive rows from the approved environment/map. Do not use the rejected candidate's oversized arch, banners, front-room shelf substitutions, extra tables, candles, statues, or invented cabinet wall. |
| Focus direction | The two walking characters are the sharp subjects; apply gradual mild depth-of-field into the deeper archive while keeping the gateway and route readable. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-28. The walking action and staff are correct, but the environment changes the established gateway/archive design: gateway proportions, archive ceiling, shelf arrangement, and far-end wall differ from approved Panels 14–15. |
| Required V3 correction | Rebuild from approved Panel 15 as the exact environment and camera authority. Preserve its gateway proportions, blue columns, arch/tunnel ceiling, visible shelf placement, stonework, sconces/statues, floor, red runner, lighting, and depth. Change only the character blocking into the Panel 16 walking beat, with Vander leading and Older Van following with exactly one canonical staff. |
| Candidate V3 audit | SOURCE APPROVED by explicit user decision on 2026-08-28. Uses a tighter threshold-crossing frame to preserve the established Panel 14–15 environment without extrapolating unseen archive depth: same human-scale blue-column gateway, stone bays, sconces/statues, shallow curved tunnel, left interior shelf, red-runner glimpse, lighting, and cel shading; Vander leads while Older Van follows with exactly one canonical staff. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export without overwriting the approved source, then place final copy and page composition in Canva. |
| Required format | Landscape 16:9 Unity final, with caption-safe space. |
| Prohibited | Reuse of the current art treatment as authority, caption, speech bubble, watermark, random writing, or treating this candidate as final. |

### Panel 17 — Vander Retrieves the Ancient Volume

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V3 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_17_SOURCE_APPROVED.png` |
| Approved SHA-256 | `e9742060bbf2de1b33786f5ff3e78eeb08b98e526179e25948b739fb489e02f1` |
| Current approval candidate | `Assets/Story/Work/Panel_17/Prologue_Panel_17_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `78d5dc828dbc54e5497dd08804d8d666a95e4dcb3c8c0078ad68e3ba230ecdff` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Environment-corrected approval candidate | `Assets/Story/Work/Panel_17/Prologue_Panel_17_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `82de21d498f6c434b7f2440f348932db6453a39a76f9f4ba5d14b47cd6452e1b` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Focus-corrected approval candidate | `Assets/Story/Work/Panel_17/Prologue_Panel_17_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `e9742060bbf2de1b33786f5ff3e78eeb08b98e526179e25948b739fb489e02f1` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 16, inside the same rear archive zone. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Move from the gateway-threshold view into the established archive aisle without changing shelf construction, stonework, red runner, or warm/cool lighting language. |
| Locked story action | Vander uses both hands to pull exactly one heavy closed ancient volume from a straight archive shelf while Older Van watches from behind carrying exactly one canonical staff. |
| Character/prop rules | Vander has no staff. Van alone carries the cyan-diamond staff. The ancient book is closed, physical, unlabelled, and non-glowing; no message or Nevali manifestation appears yet. |
| Focus direction | Vander and the partially removed volume are sharp. Older Van is readable with mild optical softness; deeper shelves receive gradual restrained depth-of-field. |
| Candidate V1 audit | REJECTED by explicit user decision on 2026-08-29. The book retrieval, characters, staff ownership, shelf, and focus are correct, but the far-left background behind Van invents a cool-blue arched room/second chamber that does not exist in the canonical library. |
| Required V2 correction | Replace only the invented background behind Van with the spatially correct reverse-facing view through the single established blue-column gateway toward the warmer front study chamber. Preserve all subjects, props, shelf geometry, composition, and style. |
| Candidate V2 audit | Intermediate only. Vander/book action, two-hand grip, Van-only staff, straight shelf, red runner, cel shading, and corrected gateway/study background are preserved, but Van and the background remain too sharp for the requested camera focus hierarchy. |
| Candidate V3 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. Vander, both hands, ancient book, and immediate shelf remain sharp; Older Van and his staff receive natural midground softness; the single gateway, warm study-side background, distant shelves, and red runner fall off further through gradual optical depth-of-field; no geometry, character, prop, lighting, or style change. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 18 — Vander Places the Ancient Volume on the Study Table

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_18_SOURCE_APPROVED.png` |
| Approved SHA-256 | `75a085b8e799d4abcada645434c106fc0f7322010d6aa4d7564629bd98e8c920` |
| Current approval candidate | `Assets/Story/Work/Panel_18/Prologue_Panel_18_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `75a085b8e799d4abcada645434c106fc0f7322010d6aa4d7564629bd98e8c920` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 17, returning to the established main study table. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Return from the archive retrieval close-up to the exact Panel 14–15 study-table camera family; preserve its table, chairs, gateway, statues, sconces, masonry, red runner, and staff resting position. |
| Locked story action | Vander uses both hands to place exactly one closed heavy ancient volume at the center of the previously empty table while Older Van watches from frame-left. |
| Character/prop rules | Vander has no staff. Van's single cyan-diamond staff rests upright beside/behind his left chair. The book must remain identical to Panel 17, closed, physical, unlabelled, and non-glowing. |
| Focus direction | Vander, both hands, and the ancient volume are sharp. Van remains clearly readable with at most slight midground softness; the gateway/archive receives restrained depth-of-field. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. Same book design and single-book count; correct two-hand Vander placement; Van's staff returns to the established far-left resting position; exact study-table/gateway environment; no open pages, magic, message, text, new prop, or Vander staff. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 19 — Vander Begins Explaining the Great War

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_19_SOURCE_APPROVED.png` |
| Approved SHA-256 | `33dc540f1cc420f02f217a6f6b97844209a8ba10ddbc31de52a76c736bd73fa8` |
| Current approval candidate | `Assets/Story/Work/Panel_19/Prologue_Panel_19_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `33dc540f1cc420f02f217a6f6b97844209a8ba10ddbc31de52a76c736bd73fa8` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 18 at the same study table. |
| Previous-panel background audit | `Reuse Exact`. Preserve Panel 18's table, closed book, chairs, staff resting position, gateway, statues, sconces, masonry, red runner, and lighting. |
| Locked story action | Vander speaks from frame-right with one restrained palm-up explanatory gesture while Older Van listens from frame-left. The Great War/entity backstory is conveyed only through later Canva dialogue, not visualized in the art. |
| Character/prop rules | Vander has no staff. Van's single cyan-diamond staff remains beside/behind the left chair. The single ancient book stays closed, stationary, non-glowing, and identical to Panels 17–18. |
| Focus direction | Vander's face, speaking hand, and the closed book are primary sharp subjects. Van remains clearly readable with only slight midground softness; the gateway/archive receives restrained depth-of-field. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. Correct same-table continuity, single closed book, Vander speaker/Van listener roles, Van-only staff ownership, cel shading, and unchanged gateway environment; no entity, war flashback, magic, message, text, new prop, or Vander staff. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 20 — Illustrated Great War Chronicle

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_20_SOURCE_APPROVED.png` |
| Approved SHA-256 | `0d1ce20c4437d3e2bdf27a1ea706ebcf05e4f0cc954b85b55665e9d2a1015d96` |
| Current approval candidate | `Assets/Story/Work/Panel_20/Prologue_Panel_20_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `58fd33eb2e80f7d929c93d7a8a00be7e861dfe50f39b2aeba03ff3ec58eb95ec` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Matched-pair approval candidate | `Assets/Story/Work/Panel_20/Prologue_Panel_20_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `0d1ce20c4437d3e2bdf27a1ea706ebcf05e4f0cc954b85b55665e9d2a1015d96` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 19 while Vander continues narrating the Great War/entity history. |
| Composition authority | User-locked book-dominant composition: elevated over-table three-quarter close-up; open book fills approximately 65–70% of the frame; partial Van at lower-left; Vander's two hands enter from above/right. |
| Locked story action | The same ancient volume is now open. Physical illustrated chronicle pages show Great War devastation on the left and the faceless black-robed entity amid abstract Broken Language corruption on the right. No magical projection or Nevali message appears yet. |
| Character/prop rules | Vander's visible sleeves are teal-and-gold; Van's partial shoulder is navy-and-gold. Vander has no staff, and Van's staff is outside the tight crop. Exactly one book appears. |
| Text rule | No readable words, titles, paragraphs, labels, numerals, pseudo-writing, or random text. History is communicated only through figurative page illustrations and abstract non-linguistic fracture motifs. |
| Focus direction | Open pages, historical illustrations, center gutter, and Vander's pointing hand are sharp. Partial Van and the table perimeter receive slight optical softness; background is cropped/defocused. |
| Candidate V1 audit | Intermediate only. Correct locked composition, book/history imagery, teal Vander sleeves/navy Van shoulder distinction, and text-free pages, but the hand choreography does not yet form the user-locked matched Panel 20–21 sequence. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. First frame of the matched pair: Vander's single active hand points unambiguously to the left-page Great War illustration; the other hand rests passively beyond the upper-right book edge; book, page art, partial Van, crop, lighting, focus, and style remain locked. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 21 — Illustrated Black-Robed Entity Chronicle

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_21_SOURCE_APPROVED.png` |
| Approved SHA-256 | `0e9b8f2db68aa6dee47ad0f0789375e4858093b2e425d7f68bb3c96828f49be3` |
| Current approval candidate | `Assets/Story/Work/Panel_21/Prologue_Panel_21_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `0e9b8f2db68aa6dee47ad0f0789375e4858093b2e425d7f68bb3c96828f49be3` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 20 as the second frame of one continuous illustrated-history explanation. |
| Locked composition | Exact same camera, open book, page illustrations, partial Van, table, lighting, crop, focus, and cel-shaded rendering as Panel 20 Candidate V2. |
| Locked story action | The same single active Vander hand shifts from the left-page Great War to point at the faceless black-robed entity on the right page. Vander's other hand remains lowered/passive beyond the upper-right book edge. |
| Character/prop rules | Teal-and-gold Vander sleeves, navy-and-gold partial Van, no staff in crop, exactly one open book, no readable text, no glow/projection, and no Nevali message yet. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. Exact matched-pair continuation: only the active pointing hand changes target from the left-page war to the right-page entity; the passive hand, book, artwork, Van, table, crop, lighting, focus, and style remain continuous. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 25 — Historical Illustrations Begin Dissolving

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_25_SOURCE_APPROVED.png` |
| Approved SHA-256 | `2431cbdcec03e5b704744f5af101f55f259f1866ffe66a07b437dde3f91911f8` |
| Current approval candidate | `Assets/Story/Work/Panel_25/Prologue_Panel_25_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `2431cbdcec03e5b704744f5af101f55f259f1866ffe66a07b437dde3f91911f8` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Planned after Panels 22–24, reusing the exact approved Panels 20–21 book-dominant composition. |
| Researched shot classification | High-angle insert shot with a partial Van foreground/POV cue. The camera is elevated and tilted down, but not a direct overhead/bird's-eye view. The insert isolates the already-established book and page transformation while matching Van's eyeline. |
| Cinematography sources | Adobe camera-shot guide: `https://www.adobe.com/ph_fil/creativecloud/video/production/cinematography/camera-shots-and-angles.html`; StudioBinder insert-shot guide: `https://www.studiobinder.com/camera-shots/framing/insert-shot/`; StudioBinder high-angle guide: `https://www.studiobinder.com/camera-shots/camera-angles/high-angle-shot/`. |
| Locked story action | The Great War and black-robed-entity illustrations partially dissolve into clean parchment through restrained cyan-white/gold page-surface motes as the book interrupts the conversation. |
| Hand continuity | Vander stops pointing; one teal-sleeved hand hovers in a restrained surprised pause while the other remains passive beyond the upper-right book edge. |
| Text rule | No readable message or pseudo-writing. Nevali's copy remains reserved for later Canva placement. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. Exact composition/book/Van/table continuity from Panels 20–21; coherent partial historical-image dissolution; clean message-safe area beginning to emerge; correct hands and teal sleeves; no text, staff, projection, Nevali figure, or new object. |

### Panel 26 — Blank Illuminated Message Pages

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-29 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_26_SOURCE_APPROVED.png` |
| Approved SHA-256 | `bbc8aa696e21f6e077f00a10770cc9c5d7563634d2700fe01867c488e5ed07ed` |
| Current approval candidate | `Assets/Story/Work/Panel_26/Prologue_Panel_26_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `5b28781ebf8e18acffd8c301a1bb4ca34689ef1e5b8edcfa35cce03db5dc7e46` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Exact-message approval candidate | `Assets/Story/Work/Panel_26/Prologue_Panel_26_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `bbc8aa696e21f6e077f00a10770cc9c5d7563634d2700fe01867c488e5ed07ed` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after planned Panel 25 as the settled message state. |
| Locked story action | All historical imagery is gone. The same open book shows clean aged parchment with restrained cold-white/cyan surface illumination and broad negative space for Nevali's message. |
| Hand continuity | Same hovering and passive Vander hands as Panel 25; no pointing and no page contact. |
| Message policy | User explicitly requires the Nevali message to be rendered directly into the source artwork rather than added in Canva. Exact copy: `Please help me. I've been trapped in this book for so long. My name is Nevali.` |
| Candidate V1 audit | REJECTED by explicit user decision on 2026-08-29 because the pages are blank and the required Nevali message cannot be deferred to Canva. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision on 2026-08-29. Exact continuation of Panel 25 with clean illuminated pages and the complete correctly spelled handwritten message rendered directly on the book: `Please help me. I've been trapped in this book for so long. My name is Nevali.` Book, hands, Van, table, crop, glow, and focus remain unchanged; no extra text, staff, projection, figure, or new object. |

### Panel 22 — Van's First Suspicion

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED v2 — Candidate V4 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_22_V2_SOURCE_APPROVED.png` |
| Approved SHA-256 | `84bd0e8ad59d89e2385474bb9eb6cbc30fd8a719c651e3337196ff769dc688ed` |
| Superseded approved asset | `Assets/Story/Approved/Prologue_Panel_22_SOURCE_APPROVED.png` — retained as archival v1 only |
| Current approval candidate | `Assets/Story/Work/Panel_22/Prologue_Panel_22_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `4cd8b3d8a4e484cc9a98dfa91298c0e89f812eeb893328738abef8375dfe996a` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Composition/staff-corrected approval candidate | `Assets/Story/Work/Panel_22/Prologue_Panel_22_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `430e7ecb4f5459e124285bc7ed159434acf2498d048bdafaac88446c0adfe335` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Exact staff-placement approval candidate | `Assets/Story/Work/Panel_22/Prologue_Panel_22_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `d82fc97916937309b8f8044500a934d4f4bcf8d24bad0dbe375b16fb0b74b664` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Cinematic dirty-OTS approval candidate | `Assets/Story/Work/Panel_22/Prologue_Panel_22_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `84bd0e8ad59d89e2385474bb9eb6cbc30fd8a719c651e3337196ff769dc688ed` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 21 and before Van directly questions Vander. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Move from the book-dominant history spread to a tight Van reaction shot at the exact same table; preserve the open illustrated book, Vander position, gateway, masonry, shelves, lighting, and room identity. |
| Locked story action | Van silently studies Vander after recognizing that the history was described with suspiciously intimate detail. His expression shows controlled doubt, not accusation, anger, or shock. |
| Character/prop rules | Van is sharp with closed mouth and narrowed attentive eyes. Vander appears only as a soft teal foreground/profile target. The same open illustrated book remains partially visible and defocused. No staff appears in the tight crop. |
| Focus direction | Van's eyes, glasses, face, hair, beard, robe, fastening, and hands are tack-sharp. Vander, open book, table depth, gateway, shelves, statues, and background receive gradual optical softness. |
| Candidate V1 audit | REJECTED by explicit user decision on 2026-08-29. Van acting/focus and environment are correct, but an awkward blurred book fragment is forced into the lower-right foreground and Van's staff is missing despite the left chair area being visible. |
| Required V2 correction | Remove the visible book fragment entirely and restore uninterrupted tabletop; add exactly one canonical staff upright beside/behind Van's left chair. Preserve Van, Vander, environment, camera, lighting, and focus. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-29. Book removal and focus are correct, but the staff visually aligns with the chair/Van area and reads as behind Van instead of beside the chair. |
| Required V3 correction | Reposition the single staff to the chair's exterior screen-left side with visible separation from Van and no overlap/alignment with the chair back post. Preserve every other element. |
| Candidate V3 audit | APPROVAL REVOKED by explicit user decision on 2026-08-31. Staff placement and Van acting are correct, but Vander's large foreground torso and the terminating table corner make him read beside Van rather than across the table. |
| Required V4 correction | Rebuild as a standard cinematic dirty OTS: camera behind Vander; only a small blurred head/shoulder sliver at extreme-right; Van sharp across the table; continuous tabletop depth and far edge proving separation; no book; staff beside chair; canonical shelf/stone background. |
| Candidate V4 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. Proper dirty OTS based on movie-shot grammar: sharp chest-up Van across the table, small blurred Vander foreground silhouette at extreme-right, continuous table plane separating them, matched eyeline, no forced book, staff beside exterior-left chair, canonical shelves/stone/sconce, and clean cel shading. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 23 — Van Directly Questions Vander

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V6 explicitly restored as canonical on 2026-08-29 after comparison with later variants |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_23_SOURCE_APPROVED.png` |
| Approved SHA-256 | `b693f70284166d263b983111ae90c8df9c603fcf03ad422b45e1304d025a3728` |
| Current approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `6d206badbe80127d183e39c549a79dd7bb34915bcf1ce9b99d88b83af0c16cef` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Opposing-seat continuity approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `2a3c31e9cafa744a941a0cc8d60bbb5f20580db7013e9e3b20a7a48fa6a106e0` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Centered-gateway approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `dcc89fcc3ec83fafbbac344b3e6036f09313f12482ccfa46584911aecad01c6b` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Vander-side-book approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `85b529657c53cea29119a77511dfac0ffddedcd87da04c1905ab3e28da5485e4` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Vander-facing-book approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V5.png` |
| Candidate V5 SHA-256 | `81fde144b3d5e4a22cb05df3ef4a9488882430f7d304a95d49a98478b7a8bd55` |
| Candidate V5 dimensions | 1672 × 941, 16:9 landscape |
| Far-wall-geometry approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V6.png` |
| Candidate V6 SHA-256 | `b693f70284166d263b983111ae90c8df9c603fcf03ad422b45e1304d025a3728` |
| Candidate V6 dimensions | 1672 × 941, 16:9 landscape |
| Clean-style matched-pair approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V7.png` |
| Candidate V7 SHA-256 | `6dfc76be1ca71abe3627aef47fbcca536d5b29e3a10404f94aa45ebcfaa88791` |
| Candidate V7 dimensions | 1672 × 941, 16:9 landscape |
| Ten-percent-darker matched-pair approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V8.png` |
| Candidate V8 SHA-256 | `ae850d6326a9f467c546c7227bf3afcbd875824245e3724ac6ee463740f84d6c` |
| Candidate V8 dimensions | 1672 × 941, 16:9 landscape |
| Five-percent-relighted matched-pair approval candidate | `Assets/Story/Work/Panel_23/Prologue_Panel_23_CANDIDATE_V9.png` |
| Candidate V9 SHA-256 | `61af77ce50dfab497184304f25e1ed4976e939d39a0103dbd5ecd3601650fb4d` |
| Candidate V9 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 22 and before Vander's guarded answer. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Return from Van's tight reaction to the established Panel 19 same-table two-shot; preserve table, open book, staff beside chair, gateway, statues, sconces, masonry, shelves, lighting, and red runner. |
| Locked story action | Van asks how Vander knows the entity so personally. Van speaks with one restrained palm-up questioning gesture; Vander listens with mouth closed and guarded discomfort. No accusation or confrontation yet. |
| Character/prop rules | Van's single staff remains beside the exterior screen-left side of his chair, not behind him. Vander has no staff. Exactly one open book remains naturally centered with the unchanged Great War/entity illustrations. |
| Focus direction | Van's face, questioning hand, and upper body are primary sharp subjects. Vander remains clearly readable with slight natural softness; the open book is secondary but recognizable; background receives restrained depth-of-field. |
| Candidate V1 audit | REJECTED by explicit user decision on 2026-08-29. Speaker/listener roles, book, staff, and environment elements are present, but the compressed angle makes Vander read as adjacent/left-side rather than clearly across the table from Van. |
| Required V2 correction | Rebuild from approved Panel 19's exact camera and opposing-seat geography: Van remains frame-left and Vander remains across the table frame-right. Only the book-showcase panels may switch to the overhead angle. Keep the single open book naturally centered and the staff beside the chair. |
| Candidate V2 audit | REJECTED by explicit user decision on 2026-08-29. Opposing-seat blocking improves, but the camera still does not use the required straight centered gateway-hall axis and therefore does not preserve the requested symmetrical environment geometry. |
| Required V3 correction | Use the old Panel 14 submission only as a centered frontal camera/blocking guide: broad table foreground, Van left, Vander opposite right, gateway hall centered. Replace its rejected window/banners/chandelier/props with the canonical blue-column gateway, statues, sconces, red runner, shelves, and stone chamber. |
| Candidate V3 audit | REJECTED by explicit user decision on 2026-08-29. Centered gateway, opposing seats, staff, and environment geometry are correct, but the open book is incorrectly relocated to the table center instead of remaining on Vander's side. |
| Required V4 correction | Move the single open illustrated book to Vander's frame-right half directly in front of him, matching Panels 18–21. Restore uninterrupted tabletop at center. Preserve camera, characters, staff, gateway, environment, and acting. |
| Candidate V4 audit | REJECTED by explicit user decision on 2026-08-29. Location on Vander's side is correct, but the open pages remain oriented toward the frontal camera instead of Vander's right-side seat. |
| Required V5 correction | Rotate the book approximately 90 degrees clockwise in place so its page-top direction faces Vander. Preserve its frame-right location, broad empty center tabletop, page art, and all other scene elements. Keep Vander's hands fixed and physically separated from the book. |
| Candidate V5 audit | REJECTED by explicit user decision on 2026-08-29. Foreground composition, staff, book ownership/orientation, and hand separation are correct, but the far archive endpoint invents a plain narrow door and omits the required upper blue mirror/window feature. |
| Required V6 correction | Replace only the distant archive endpoint with one unified centered unit: dark wooden double door below and blue arched mirror/window/fanlight directly above, same outer width, aligned edges, unified stone frame, symmetrical shelf termination, and runner ending at the door. Preserve all foreground elements. |
| Candidate V6 audit | SOURCE APPROVED. The user explicitly restored V6 as canonical on 2026-08-29 after reviewing the later clean/brightness comparison variants. Centered gateway, merged far double-door/fanlight, staff, Vander-facing book, safe hand separation, acting, geometry, and original darker treatment are accepted. |
| Required V7 correction | Preserve every object and pose while cleaning the whole-frame rendering against Panels 18–19 and Panel 1 V2: brighter midtones, cleaner navy/teal separation, lighter skin/hair, simplified cel shadows, reduced oily brown-black glaze and surface noise, and cleaner book/table/masonry rendering. |
| Candidate V7 audit | Intermediate clean-style correction. Exact composition and continuity are preserved with brighter clean cel shading and reduced grease/noise, but user requested the matched pair be uniformly 10% darker. |
| Candidate V8 audit | Intermediate matched-pair exposure pass. Clean style and continuity are preserved at approximately 10% below V7, but user requested a 5% increase from this state. |
| Candidate V9 audit | Non-canonical comparison variant. Candidate V6 was explicitly restored instead; V7–V9 remain archived and must not replace the approved source. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 24 — Vander's Guarded Answer

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 selected and promoted on 2026-08-29 after comparison with later variants |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_24_SOURCE_APPROVED.png` |
| Approved SHA-256 | `1f3cbef440d19adefc2a6f31efb59211cc4a546a9b49594b79d97c0fa61c96ee` |
| Current approval candidate | `Assets/Story/Work/Panel_24/Prologue_Panel_24_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `1f3cbef440d19adefc2a6f31efb59211cc4a546a9b49594b79d97c0fa61c96ee` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Clean-style matched-pair approval candidate | `Assets/Story/Work/Panel_24/Prologue_Panel_24_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `b3fe3286dedf1d45831b5128668ff829b03f167e3501fe950542d78719216031` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Ten-percent-darker matched-pair approval candidate | `Assets/Story/Work/Panel_24/Prologue_Panel_24_CANDIDATE_V3.png` |
| Candidate V3 SHA-256 | `cb984ddb4283893e8cdf4a901c57392b2790495e4c781aea795c7e8b6b48394b` |
| Candidate V3 dimensions | 1672 × 941, 16:9 landscape |
| Five-percent-relighted matched-pair approval candidate | `Assets/Story/Work/Panel_24/Prologue_Panel_24_CANDIDATE_V4.png` |
| Candidate V4 SHA-256 | `80cb2e558a4bf8e68f316276064aec08542cf1422aa2fc3d45a49912615cae08` |
| Candidate V4 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 23 and before the book-history dissolution in planned Panel 25. |
| Previous-panel background audit | `Reuse Exact`. Preserve Panel 23's centered camera, gateway, shelves, runner, merged far double-door/fanlight, statues, sconces, table, chairs, staff, and Vander-facing book. |
| Locked story action | Vander gives a guarded answer from frame-right, claiming their paths crossed and that he witnessed the entity's fall. Van listens frame-left with mouth closed and suspicious attention. |
| Character/hand rules | Vander has one small hand-near-chest qualifying gesture while his other hand stays on his side of the table. Van's hands rest on his own side. All hands remain physically separated from the book. |
| Prop rules | The single open illustrated book remains on Vander's frame-right side, rotated toward him; staff remains beside Van's exterior-left chair; Vander has no staff. |
| Focus direction | Vander is the primary sharp speaker. Van remains clearly readable with mild softness; book is secondary; centered background geometry receives restrained depth-of-field. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-29 after the user reconsidered the later brightness/style revisions. Exact Panel 23 camera/geometry, merged far door/fanlight, opposing seats, role reversal, Vander-facing book, safe hand-book separation, beside-chair staff, and original darker treatment are accepted. |
| Required V2 correction | Derive directly from cleaned Panel 23 V7 and change only speaking roles/hand acting. Preserve exact exposure, palette, line weight, book art, geometry, staff, hands/book separation, and clean cel shading. |
| Candidate V2 audit | Intermediate clean-style correction. Exact cleaned Panel 23 V7 camera, geometry, palette, book art, staff, and acting continuity are preserved, but user requested the matched pair be uniformly 10% darker. |
| Candidate V3 audit | Intermediate matched-pair exposure pass. Clean style and continuity are preserved at approximately 10% below V2, but user requested a 5% increase from this state. |
| Candidate V4 audit | Non-canonical comparison variant. Candidate V1 was explicitly selected instead; V2–V4 remain archived and must not replace the approved source. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final copy/page placement in Canva without overwriting the approved source. |

### Panel 27 — Van Recognizes Nevali's Name

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_27_SOURCE_APPROVED.png` |
| Approved SHA-256 | `c8afe6103a0c27a3187987f722c8e139dad6105c65d8ec7fa902808416d0f010` |
| Current approval candidate | `Assets/Story/Work/Panel_27/Prologue_Panel_27_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `c8afe6103a0c27a3187987f722c8e139dad6105c65d8ec7fa902808416d0f010` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 26's handwritten Nevali message and before Van's confrontation. |
| Researched shot classification | Dirty OTS reaction close-up with a subtle push-in from Panel 22 v2's established camera position. Vander remains a blurred extreme-right framing sliver; Van is the sharp shoulders/upper-chest reaction subject across the table. |
| Cinematography sources | Adobe close-up guide: `https://www.adobe.com/creativecloud/video/production/cinematography/camera-shots-and-angles/close-up-shot.html`; StudioBinder reaction-shot guide: `https://www.studiobinder.com/blog/what-is-a-reaction-shot-definition/`; Adobe action/reaction sequence guide: `https://www.adobe.com/creativecloud/video/production/cinematography/camera-shots-and-angles/sequence-shot.html`. |
| Camera/axis lock | Same side of the 180-degree axis as Panel 22 v2; camera behind/slightly beside Vander at eye/shoulder level; Van faces screen-right toward him. |
| Locked story action | Van silently recognizes Nevali's name. Slightly widened blue eyes, caught breath, subtly parted mouth, and dawning hurt/disbelief appear before anger or confrontation. |
| Prop/crop rules | Thin soft table strip only; no book/message repetition; staff legitimately outside the tighter crop; no visible Vander staff. |
| Focus direction | Van's eyes, glasses, face, hair, beard, and navy/gold collar are tack-sharp. Vander's small foreground sliver and canonical shelf/stone/sconce background are naturally defocused. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. Strong researched dirty-OTS reaction close-up, preserved axis/eyeline and canonical Van identity, recognition-before-anger acting, small blurred Vander framing, clean table separation, no forced book/staff, coherent shelf/stone background, and clean cel shading. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final page placement/copy without overwriting the approved source. |

### Panel 28 — Van Confronts Vander

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V5 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_28_SOURCE_APPROVED.png` |
| Approved SHA-256 | `4b7ff248e660985c35bc6e6b15993bd39d08407724cf59e06c6cfa506b4108b1` |
| Current approval candidate | `Assets/Story/Work/Panel_28/Prologue_Panel_28_CANDIDATE_V1.png` |
| Candidate V1 SHA-256 | `f9130a6dd16b7770179c66e1312bb9bd6c585d2714a23d48ca3a813a7ab974a4` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Exact-message/Van-facing approval candidate | `Assets/Story/Work/Panel_28/Prologue_Panel_28_CANDIDATE_V2.png` |
| Candidate V2 SHA-256 | `4b7ff248e660985c35bc6e6b15993bd39d08407724cf59e06c6cfa506b4108b1` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after approved Panel 27 and before Vander explains the living spellbook prison. |
| Researched shot classification | Table-level subtly low-angle dirty OTS medium shot. Same 180-degree-axis side as Panels 22/27; camera behind Vander and lowered just above the near tabletop to show Van's face, torso, and both planted hands while signaling a power shift. |
| Cinematography sources | Adobe low-angle guide: `https://www.adobe.com/in/creativecloud/video/production/cinematography/camera-shots-and-angles/low-angle-shot.html`; Adobe medium-shot guide: `https://www.adobe.com/creativecloud/video/hub/ideas/what-is-a-medium-shot-in-film.html`; StudioBinder low-angle guide: `https://www.studiobinder.com/camera-shots/camera-angles/low-angle-shot/`; StudioBinder medium-shot guide: `https://www.studiobinder.com/camera-shots/shot-size/medium-shot/`. |
| Locked story action | Van rises/leans across the table, plants both hands on his far half, and forcefully demands why Nevali is inside the book. Grief turns into anger/betrayal without magical impact or caricature. |
| Table/book geometry | Broad table separates the characters. Van's hands remain on his far side. The single open message book stays softly defocused on Vander's near/right side, rotated toward him, with the exact Nevali message and broad wood separation from all hands. |
| Character/prop rules | Van's canonical staff remains beside the exterior screen-left side of his chair. Vander is only a small blurred right-foreground sliver and has no visible staff. Both Van hands have five correct fingers and clean contact shadows. |
| Focus direction | Van's face, eyes, torso, and both hands are tack-sharp. Vander and the foreground book are softly defocused but recognizable; shelf/stone/sconce background is blurred. |
| Candidate V1 audit | REJECTED by explicit user decision on 2026-08-31. Shot/action/anatomy are correct, but the foreground book reflows the approved Panel 26 message and orients the writing toward Vander/camera instead of Van. |
| Required V2 correction | Replace the foreground book with the exact approved Panel 26 spread and rotate the complete book/message 180 degrees so it faces Van across the table. From the camera behind Vander, the correct message must appear upside down/foreshortened. Preserve all other content. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. Strong researched low-angle dirty-OTS confrontation; correct five-finger hands; exact Panel 26 message layout preserved; book remains on Vander's side but rotates 180 degrees to face Van, appearing upside down to camera; broad hand separation, staff beside chair, canonical background, and clean cel shading remain intact. |
| Remaining technical work | After source approval, create a separate standardized 1920×1080 Unity export and assemble final page placement/copy without overwriting the approved source. |

### Panel 29 — Vander Begins the Living-Spellbook Explanation

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V6 canonical environment correction promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_29_SOURCE_APPROVED.png` |
| Approved SHA-256 | `e902a8514a3c324f07d2d00fddcc380f7228e6637e4186090262bedf5fec91eb` |
| Initial candidate | `Assets/Story/Work/Panel_29/Prologue_Panel_29_CANDIDATE_V1.png` |
| Initial candidate SHA-256 | `0b248046429e49eb4d50039a2685b2507e0145f4e61f878823e4696cd2e1f099` |
| Superseded focus-corrected candidate | `Assets/Story/Work/Panel_29/Prologue_Panel_29_CANDIDATE_V2.png` |
| Rejected generic shelf-wall candidate | `Assets/Story/Work/Panel_29/Prologue_Panel_29_CANDIDATE_V4.png` |
| Approved canonical-environment candidate | `Assets/Story/Work/Panel_29/Prologue_Panel_29_CANDIDATE_V6.png` |
| Candidate V6 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 28's confrontation and before the deeper spellbook-prison explanation/forbidden-technique decision. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. The dirty-OTS camera moves behind Van and turns across the table toward Vander; this direction faces Vander's side-wall bookshelf, not Panel 23's frontal gateway. Reuse Panel 8's exact approved shelf wall at its original normalized screen arrangement. |
| Background authority | Approved Panel 8 shelf wall: left-offset navy compass banner centered around 39–40% frame width, twin-candle sconces around 21–22% and 74–75%, asymmetric wooden shelf-bay/upright/cabinet rhythm, dense dark books, and warm amber shelf lighting. No gateway, columns, statues, runner, archive corridor, distant door, or fanlight may appear. |
| Researched shot classification | Slightly high-angle reverse dirty OTS medium close-up from behind Van. The modest high angle visually reduces Vander's authority after Panel 28's low-angle power shift without becoming an overhead insert. |
| Locked story action | Vander absorbs Van's accusation and begins a guarded, regretful explanation that the ancient volume is a living spellbook-prison. One hand makes a restrained explanatory gesture while the other rests safely on his side of the table. |
| Locked Canva dialogue | Vander: `Van… this is no ordinary volume.` |
| Table/book geometry | The single open message book remains on Vander's side but faces Van. The exact Panel 26 message is preserved and all hands remain physically separated from the pages. |
| Focus direction | Vander is tack-sharp. Van's extreme-left foreground hair, face edge, shoulder, robe, and partial arm are strongly optically defocused; the blur does not spill onto the table, book, or Vander. |
| Candidate V1 audit | Composition, axis, acting, environment, anatomy, and message orientation passed, but the user requested stronger foreground Van defocus. |
| Candidate V2 audit | Superseded after final continuity audit: focus, characters, book, and acting were approved, but the reverse angle retained the physically impossible Panel 23 gateway background. |
| Candidate V4 audit | REJECTED because it generated a merely similar shelf wall instead of retaining Panel 8's actual approved background arrangement. |
| Candidate V6 audit | SOURCE APPROVED by explicit user decision. Panel 29's camera/foreground/book/acting remain fixed while Panel 8's left-offset banner, sconce positions, asymmetric shelf bays, cabinet seams, book rows, and wood treatment replace the gateway background. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final page placement/copy without overwriting the approved source. |

### Panel 30 — The Living-Prison Revelation

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_30_SOURCE_APPROVED.png` |
| Approved SHA-256 | `5a483c8b9990d1dfa20030132f4a72bd616018a6f02fa808fb5fd7904fb2f0b3` |
| Approved candidate | `Assets/Story/Work/Panel_30/Prologue_Panel_30_CANDIDATE_V1.png` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 29 and before Van demands a way into the book, beginning the forbidden-technique decision. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. A motivated insert stays on Van's side of the established 180-degree axis and reuses the same table, book, Van-side shelf/stone/sconce background, lighting, and prop positions. |
| Researched shot classification | Over-table book-dominant reaction insert. The single open book fills the lower frame while Van remains visible beyond it so the critical object and emotional consequence share the composition. |
| Cinematography sources | Adobe cutaway guide: `https://www.adobe.com/creativecloud/video/production/cinematography/camera-shots-and-angles/cut-away-shot.html`; StudioBinder insert-shot guide: `https://www.studiobinder.com/camera-shots/framing/insert-shot/`. |
| Locked story action | Vander's off-camera explanation reveals that the volume contains a real prison-world. Van's anger collapses into horrified realization as he understands that Nevali is trapped inside. |
| Locked Canva dialogue | Vander, off-panel: `It is a living prison—an entire world sealed within its pages. If Nevali wrote this message… then she is trapped inside.` |
| Book/message rules | Exactly one open book, facing Van. Preserve the exact Panel 26 message and layout: left page `Please help me.`; right page `I've been trapped / in this book for / so long. / My name is Nevali.` No map, portal, prison landscape, added writing, or projection. |
| Character/hand rules | Van looks down at the book with restrained dread. Only one complete teal-and-gold Vander hand enters from screen-right, with five correct fingers and broad separation from the pages. Van's staff remains beside the exterior-left side of his chair. |
| Focus direction | Book and Van's eyes share the narrative focal plane. Vander's entering hand and the table perimeter are secondary; the unchanged library background is naturally defocused. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. Exact message, book orientation, safe hand separation, beside-chair staff, Van identity/reaction, canonical environment, clean cel shading, and absence of invented spellbook imagery are accepted. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 31 — Van Demands a Way Inside

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_31_SOURCE_APPROVED.png` |
| Approved SHA-256 | `62f02a4493428473ab616f420563f5fd69243eda84fe4f16b08b21a4f9196806` |
| Approved candidate | `Assets/Story/Work/Panel_31/Prologue_Panel_31_CANDIDATE_V1.png` |
| Candidate V1 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 30's living-prison revelation and before Vander warns that entry requires a forbidden technique. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Motivated push-in to a clean Van single from Vander's side of the established 180-degree axis. |
| Researched shot classification | Eye-level clean decision close-up, framed as the endpoint of a subtle push-in. |
| Cinematography sources | Adobe close-up guide: `https://www.adobe.com/creativecloud/video/production/cinematography/camera-shots-and-angles/close-up-shot.html`; StudioBinder push-in guide: `https://www.studiobinder.com/camera-shots/camera-movements/push-in-shot/`. |
| Locked story action | Van raises his gaze from the book toward Vander. Fear and grief consolidate into quiet, unwavering determination. |
| Locked Canva dialogue | Van: `Then tell me how to get inside.` |
| Crop/prop rules | Van only; no Vander foreground, book fragment, table strip, hands, or forced staff. Book and staff remain correctly placed outside the tight crop. |
| Focus direction | Van's blue eyes, glasses, face, hair, beard, and upper navy/gold robe are tack-sharp. The unchanged Van-side aligned shelves, stone masonry, and sconce receive restrained optical defocus. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision on 2026-08-31. The shot reads as controlled resolve rather than renewed anger, preserves the dialogue axis and Van identity, and introduces no prop or environment drift. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 32 — Vander Warns of the Forbidden Technique

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_32_SOURCE_APPROVED.png` |
| Approved SHA-256 | `979eacbed00275fca430e1bd7c62b2097009445926b4d4df91333f70116ef864` |
| Rejected initial candidate | `Assets/Story/Work/Panel_32/Prologue_Panel_32_CANDIDATE_V1_REJECTED.png` |
| Approved candidate | `Assets/Story/Work/Panel_32/Prologue_Panel_32_CANDIDATE_V2.png` |
| Candidate V2 dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 31 and before Van explicitly accepts the forbidden technique. |
| Previous-panel background audit | `Reuse With Justified Camera Change`. Return to a wider two-shot on the same dialogue axis to show Vander standing and the canonical gateway as his next movement destination. |
| Researched shot classification | Asymmetric eye-level medium-wide two-shot from the end/side of the table. |
| Cinematography sources | StudioBinder two-shot guide: `https://www.studiobinder.com/camera-shots/framing/two-shot/`; Adobe medium-shot guide: `https://www.adobe.com/creativecloud/video/hub/ideas/what-is-a-medium-shot-in-film.html`. |
| Locked story action | Vander rises screen-right, turns partly toward the gateway/archive, and looks back gravely toward seated Van while revealing that entry requires a forbidden technique. |
| Locked Canva dialogue | Vander: `There is a way. But it requires a forbidden technique—one no wizard is permitted to use.` |
| Book/prop rules | One open book remains on Vander's half and is rotated toward Van's screen-left seat. Van's staff remains beside the exterior-left side of his chair. Vander has no staff yet. |
| Candidate V1 audit | REJECTED internally before presentation because the book faced the frontal camera instead of Van, repeating a previously rejected continuity error. |
| Candidate V2 audit | SOURCE APPROVED through the user's explicit proceed instruction on 2026-08-31. The localized correction rotates the book toward Van while preserving character blocking, safe anatomy, staff placement, gateway geometry, lighting, and style. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 33 — Van Accepts the Forbidden Technique

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — user-selected wider profile two-shot promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_33_SOURCE_APPROVED.png` |
| Approved SHA-256 | `b195591eed2f0930c6b366d230dde1ab51edf9942fecd887a8ef85772e0a9faa` |
| Approved full-resolution candidate | `Assets/Story/Work/Panel_33/Prologue_Panel_33_CANDIDATE_V4_USER_SELECTED_WIDE_PROFILE.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| User approval evidence | The user attached an 890 × 485 clipboard preview of this wider profile composition and explicitly approved it before stopping work. The canonical asset preserves the matching full-resolution generated source rather than the reduced clipboard image. |
| Story placement | Immediately after Panel 32's warning and before Vander's staff-retrieval transition. |
| Researched shot classification | Profile-leaning medium two-shot with both standing characters sharing one reciprocal sight line. |
| Locked story action | Van stands screen-left and accepts the danger; Vander stands screen-right and receives the decision. Their head turns, nose directions, and inward pupil vectors visibly connect across the table. |
| Locked Canva dialogue | Van: `Forbidden or not, I will not leave my daughter in there.` |
| Prop/environment rules | The single book remains on Vander's half; Van's blue-crystal staff remains beside the exterior-left side of his chair; Vander still has no staff. Canonical gateway/archive geometry remains centered and unchanged. |
| Rejected variants | V1 and V2 failed because Van's eyes looked past Vander despite general inward head orientation. The later tight-crop comparison was not the user-selected approval source. |
| Approved audit | SOURCE APPROVED by explicit user decision. Wider profile staging, direct eye contact, character identities, gateway geometry, table/book/staff continuity, and clean cel-shaded treatment are accepted. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 34 — Vander Leaves to Retrieve His Staff

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_34_SOURCE_APPROVED.png` |
| Approved SHA-256 | `5324a2094d8b140cfddb7093efe78cfc66263ca49b8d789ba9626e8a658c459e` |
| Approved candidate | `Assets/Story/Work/Panel_34/Prologue_Panel_34_CANDIDATE_V1.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 33 and before Panel 35's return carrying the approved distinct staff. |
| Researched shot classification | Centered one-point-perspective departure wide shot, framed like a rear tracking-shot moment. |
| Cinematography sources | SHOT.CAFE one-point perspective example: `https://shot.cafe/movie/se7en-1995/one-point-perspective`; Adobe wide-shot guide: `https://www.adobe.com/creativecloud/video/production/cinematography/camera-shots-and-angles/wide-shot.html`; Adobe tracking-shot guide: `https://www.adobe.com/uk/creativecloud/video/production/cinematography/camera-shots-and-angles/tracking-shot.html`. |
| Locked story action | Empty-handed Vander walks away from camera one to three steps beyond the blue-column gateway threshold along the red runner. Van remains foreground-left in the study and watches him leave. |
| Locked Canva dialogue | Vander: `Wait here. I must retrieve my staff.` |
| Staff rules | Vander's approved staff is not visible yet. Van's blue-crystal staff remains beside the exterior-left side of Van's chair. |
| Book/environment rules | One open book remains on Vander's former table half and is rotated toward Van. Preserve the centered gateway, blue columns, paired statues/sconces, runner, archive shelves, and unified distant door/fanlight. |
| User approval evidence | The user explicitly approved an 887 × 505 clipboard preview matching this composition. The canonical asset preserves the full-resolution candidate instead of the reduced preview. |
| Candidate V1 audit | SOURCE APPROVED. Correct empty-handed departure, threshold distance, one-point corridor geometry, Van waiting in study, beside-chair staff, Van-facing book, warm/cool separation, and clean cel-shaded style. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 35 — Vander Returns With His Staff

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_35_SOURCE_APPROVED.png` |
| Approved SHA-256 | `f8a20c82247674fc321ff0e6c32991792588ed201d6e482bd8fbd52fa6aa0be3` |
| Initial oversized candidate | `Assets/Story/Work/Panel_35/Prologue_Panel_35_CANDIDATE_V1.png` |
| Approved candidate | `Assets/Story/Work/Panel_35/Prologue_Panel_35_CANDIDATE_V2.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Exact-camera matched return after Panel 34 and before Panel 36's forbidden casting technique. |
| Researched shot classification | Centered one-point-perspective matched return/reveal shot using Panel 34's exact camera and reverse movement. |
| Locked story action | Vander walks toward the study at the gateway threshold carrying his distinct staff for the first time. Van remains foreground-left watching. No casting has begun. |
| Staff design lock | Twisted dark wood, aged-gold fittings, complete circular cage, four cardinal points, one pale oval inactive crystal, and bottom ferrule. Van's separate blue-diamond/open-prong staff remains beside his chair. |
| Staff scale lock | Total Vander-staff height is approximately equal to Vander's standing height. Ferrule rests at or just above floor level; cage top aligns with his hair crown, with only negligible clearance. The cage/crystal must not be enlarged and the staff may not tower above him in later panels. |
| Book/environment rules | Preserve the Van-facing single open book, table, Van, chair, Van staff, centered gateway, statues, sconces, runner, shelves, and distant door/fanlight from Panel 34. |
| Candidate V1 audit | REJECTED because Vander's staff towered too far above his head and read oversized. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision. Correct matched return, distinct inactive staff, crown-level cage top, floor-safe ferrule, unchanged Van staff, book, environment, lighting, and cel-shaded style. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final page placement without overwriting the approved source. |

### Panel 36 — Forbidden Technique Activation

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — revised Candidate V3 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_36_SOURCE_APPROVED.png` |
| Approved SHA-256 | `1ec7128fa04f479ae538d3885743b3ec55cb1b06bdfb2d53bd1ed6d5f2a8957e` |
| Superseded approved candidate | `Assets/Story/Work/Panel_36/Prologue_Panel_36_CANDIDATE_V1.png` |
| Approved revision candidate | `Assets/Story/Work/Panel_36/Prologue_Panel_36_CANDIDATE_V3.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 35 and before Panel 37's physical transportation into the book. |
| Researched shot classification | Centered ceremonial medium-wide three-quarter casting shot with a clear staff-to-book-to-recipient action triangle. |
| Composition/story reference | The user's educational-game reference controls only the tool-activation → book-destination → transportation progression; its characters, comic layout, pen, purple effect, speech bubbles, and environment are not reused. |
| Locked story action | Vander activates the forbidden technique from screen-right. A controlled white-cyan/gold filament runs from his staff cage to the open book; a compact storybook portal rises from the gutter; Van holds his canonical staff and receives only targeting rim-light/air pull while remaining fully physical. |
| Locked Canva dialogue | Vander: `Prepare yourself. Once the spell begins, I cannot stop it.` |
| Staff rules | Vander: approved circular-cage/pale-oval staff, total height approximately equal to Vander, cage at crown, lower shaft occluded behind table. Van: approved narrow symmetric light-cyan diamond, compact gold cradle, dark-brown shaft, held in one five-finger hand; no duplicate remains beside the chair. |
| Book rules | Exactly one open book remains on Vander's half and is angled toward Van. Preserve Nevali's message, restrained page glow, and controlled energy endpoint; no second book, pseudo-writing, map, or new symbols. |
| Magic rules | Controlled cold-white/cyan with aged-gold edges only. Compact book-anchored storybook funnel is allowed. No purple, floor circle, random runes, explosion, smoke, occult symbols, or body transport in this frame. |
| User approval evidence | The user explicitly approved an 877 × 485 clipboard preview matching this final composition. The canonical asset preserves the full-resolution source. |
| Candidate V1 audit | Superseded but not rejected: approved pre-portal setup in which Van's staff remained beside the chair. |
| Candidate V2 audit | REJECTED because Van's held staff drifted into an oversized broad blue kite/spearhead instead of the approved compact cyan diamond. |
| Candidate V3 audit | SOURCE APPROVED by explicit user decision. Correct canonical held Van staff, no duplicate, crown-height Vander staff, storybook portal anchored to the book, intact Van, canonical environment, and clean cel-shaded style. |
| Remaining technical work | Create a separate standardized 1920×1080 Unity export and assemble final dialogue/page placement without overwriting the approved source. |

### Panel 37 — Van Enters the Living Spellbook

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V3 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_37_SOURCE_APPROVED.png` |
| Approved SHA-256 | `2c8d388873f47532ebbb8c050c6c3f1bc7ab96fe3f0b5e3c7fa580ea7967b1af` |
| Rejected initial candidate | `Assets/Story/Work/Panel_37/Prologue_Panel_37_CANDIDATE_V1.png` |
| Rejected shock-only candidate | `Assets/Story/Work/Panel_37/Prologue_Panel_37_CANDIDATE_V2.png` |
| Approved candidate | `Assets/Story/Work/Panel_37/Prologue_Panel_37_CANDIDATE_V3.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Final library-location panel, immediately after Panel 36's approved storybook-portal activation and before the pending Tutorial-map awakening handoff. |
| Researched visual treatment | Controlled smear/stretch teleportation: one continuous recognizable Van silhouette progressively narrows into an S-shaped navy/gold/cyan ribbon entering the portal/book gutter. |
| Locked story action | Shocked Van is approximately 60–75% pulled into the book. His chin, nose, irises, and pupils angle down-right toward the storybook portal. His upper face remains recognizable while his robe/body becomes the continuous transfer ribbon. |
| Van-staff continuity | Van retains one canonical narrow cyan-diamond/compact-gold-cradle staff in a five-finger grip. It travels as a separate rigid object aligned with the pull and does not remain beside the chair, duplicate, melt into Van, or become Vander's staff. |
| Vander/staff continuity | Vander stays fully solid in the library and controls the spell with his approved crown-height circular-cage/pale-oval staff. |
| Book/magic rules | Exactly one active Van-facing book; compact white-cyan/gold portal anchored in the center gutter; physical pages lift under suction; no purple, floor circle, random runes, gore, tentacles, duplicate Van, or occult imagery. |
| Candidate V1 audit | REJECTED because Van's expression remained too calm for involuntary spaghettification. |
| Candidate V2 audit | REJECTED because Van looked upward/outward instead of toward the portal pulling him. |
| Candidate V3 audit | SOURCE APPROVED by explicit user decision. Shocked portal-directed gaze, continuous body ribbon, correct traveling Van staff, stable Vander/staff, book portal, environment, and cel-shaded style are accepted. |
| Remaining technical work | Create Tutorial-map wake-up/handoff Panels 38–39, then complete the full continuity audit, lock dialogue/captions/page turns, assemble Canva comic pages, and create standardized Unity-final exports. |

### Panel 38 — Van Awakens Inside the Book

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_38_SOURCE_APPROVED.png` |
| Approved SHA-256 | `dfa5a82cf4ef6a5892c48ae932bbaa53b0ae9d21f087fea3c66c908f4ed92055` |
| Initial short-staff candidate | `Assets/Story/Work/Panel_38/Prologue_Panel_38_CANDIDATE_V1.png` |
| Approved candidate | `Assets/Story/Work/Panel_38/Prologue_Panel_38_CANDIDATE_V5.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 37's book entry and before Panel 39's first-person sunlight POV. |
| Environment authority | User-provided TutorialScene spawn screenshots: flat dark-green clearing, stacked-cone evergreen trees, faceted brown trunks, gray angular rock/slopes, dense smaller trees, and blue-grey sky. |
| Researched shot classification | Ground-level three-quarter medium-wide wake-up shot. |
| Locked story action | Van lies on his side/back at the spawn clearing, supports himself on one forearm, squints into warm sunlight, and begins to rise with dazed cautious wonder. |
| Staff rules | Van's transported canonical staff lies intact and inactive on the grass within reach. Full physical length is approximately Van's standing height; narrow cyan diamond and compact gold cradle remain unchanged. |
| Candidate V1 audit | REJECTED because the lying staff was visibly too short relative to Van. |
| Candidate V2 audit | Previously source approved, then reopened for improved sunlight-response acting and foreground light continuity. |
| Candidate V5 audit | SOURCE APPROVED by explicit user decision. Preserves the full-length staff and TutorialForest composition while locking Van's squint, direct sunlight on his face/raised hand, and coherent dappled sunlight across the foreground grass and staff. |
| Future production note | The approved character rendering may guide a future 3D Van remake and staff-model visual update. This is proposed future asset work only; no current Unity model has been changed or validated against it. |
| Remaining technical work | Include this locked asset in the complete Panels 1–42 continuity/page-assembly/Unity-export audit. |

### Panel 39 — First-Person Sunlit Awakening

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_39_SOURCE_APPROVED.png` |
| Approved SHA-256 | `ae8d52353421a6edadbc88e4cadb35b4f407482647615a627929b4d59e1e10a4` |
| Approved candidate | `Assets/Story/Work/Panel_39/Prologue_Panel_39_CANDIDATE_V2.png` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Story placement | Immediately after Panel 38's exterior wake-up shot and before Panel 40's standing cleanup action. |
| Environment authority | Approved Panel 38 TutorialForest plus user-provided spawn/forest references. |
| Researched shot classification | Supine first-person point-of-view shot looking upward from ground level. |
| Locked story action | From Van's own eyes, both hands rise into frame as one shields him from the bright sun; clouds and low-poly conifers frame the sky. |
| Staff rules | The staff is intentionally absent because this is Van's direct upward POV. No rod, wand, shaft, crystal, or staff shadow may appear. |
| Candidate V1 audit | Previously source approved as the rear route-handoff composition, then retired from the final sequence when Panels 40–41 were added. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision. Correct lying-down POV, hand anatomy, sunlight response, clouds, TutorialForest trees, and intentional absence of the staff. |
| Remaining technical work | Include this locked asset in the complete Panels 1–42 continuity/page-assembly/Unity-export audit. |

### Panel 40 — Brushing Off the Robe

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V1 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_40_SOURCE_APPROVED.png` |
| Approved candidate | `Assets/Story/Work/Panel_40/Prologue_Panel_40_CANDIDATE_V1.png` |
| Approved SHA-256 | `6a478d526188b3813ec1ec3efdfce8eb9f4ab7bfcdea0eb3f07aeedacc34b3cb` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Locked story action | Van stands in the clearing, looks down at his robe, holds a fold steady, and brushes away visible dirt, dust, and grass particles. His full-length staff remains intact on the grass so both hands are free. |
| Candidate V1 audit | SOURCE APPROVED by explicit user decision. Correct downward gaze, readable two-hand robe-cleaning action, visible dirt/grass release, intact full-length staff, and TutorialForest continuity. |

### Panel 41 — Van Retrieves His Staff

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — Candidate V2 promoted on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_41_SOURCE_APPROVED.png` |
| Approved candidate | `Assets/Story/Work/Panel_41/Prologue_Panel_41_CANDIDATE_V2.png` |
| Approved SHA-256 | `e8ca98564c03682ee382335721b55a0b261052f4497b1abe8826da2a527e839a` |
| Dimensions | 1672 × 941, 16:9 landscape |
| Locked story action | Van bends down, looks directly at his grip, braces one hand on his knee, and begins lifting his full-length staff from the grass. Panel 42 follows with the route/bridge handoff. |
| Candidate V2 audit | SOURCE APPROVED by explicit user decision. Correct pickup action, staff-focused eyeline, braced anatomy, single full-length staff, and clean final TutorialForest beat. |
| Retired candidate | Candidate V1 preserves the former rear route/bridge handoff artwork but is no longer part of the planned sequence. It remains archived and was not deleted. |

### Panel 42 — Tutorial Route and Bridge Handoff

| Field | Value |
| --- | --- |
| Status | SOURCE APPROVED — previously approved rear-route artwork restored unchanged as Panel 42 on 2026-08-31 |
| Canonical approved asset | `Assets/Story/Approved/Prologue_Panel_42_SOURCE_APPROVED.png` |
| Approved candidate | `Assets/Story/Work/Panel_42/Prologue_Panel_42_CANDIDATE_V1.png` |
| Approved SHA-256 | `e312c8ba186699f53e49fd7c679e1cc14f4712494e3168a2cb5eed5e9136f0f8` |
| Dimensions | 1672 × 940, 16:9 landscape |
| Story placement | Final illustrated Story-to-Tutorial handoff after Panel 41's staff retrieval and immediately before the live `IntroWelcome` state. |
| Locked story action | Van stands left-of-center from the rear with his full-length staff and faces the quiet forest route toward the partially obscured low bridge. |
| Environment authority | User-provided TutorialForest route/bridge screenshots: dense stacked-cone trees and bushes, grass clearing, angular rocks, and a modest low brown bridge partly obscured in the midground. |
| Route/bridge rules | The route bends subtly forward/center-right. No paved road, grand bridge, river vista, shrine, village, or invented landmark. |
| Staff rules | One canonical full-length Van staff, dark-brown shaft, compact gold cradle, narrow cyan diamond, and natural grip. |
| Approval history | The image was explicitly approved in its former Panel 39 placement, preserved unchanged when the ending sequence expanded, and restored as Panel 42 after the user identified the missing walking/bridge handoff. |

## Rejected Assets

### Panel 09 transparent-background attempt

- Status: REJECTED by explicit user decision.
- The submitted file visually contains a checkerboard but is RGB with no alpha
  channel; it is not a valid transparent foreground layer.
- Do not use it as a character, prop, composition, environment, or continuity
  reference.

## Pending Inventory

- Panel 02
- Panel 03
- Panel 04
- Panel 05
- Panel 06
- Panel 07
- Panel 08
- Panel 09 final rebuild
- Panel 10 final rebuild
- Panel 11
- Panel 12 and later continuation
