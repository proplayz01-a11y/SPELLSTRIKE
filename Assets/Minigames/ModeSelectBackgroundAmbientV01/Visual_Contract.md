# Background ambience contract

Scope: the approved purple/gold tent backdrop only. Source scene and all card/navigation bindings are preserved apart from the existing background Image's sprite and new owned children beneath it.

- Two native lantern cutouts, main ceiling pivots at source (348,0)/(1328,0), maximum sway 2.2 degrees, periods 9.2/10.7 seconds, asynchronous phases 0.07/0.61.
- Two separately extracted secondary chains retain fixed upper anchors and follow their moving painted roof attachments. Main chains remain in their native body cutouts. No extra lantern or candle geometry remains baked beneath them.
- Three native candle flames pivot at source (231,504), (261,481), (1392,516). Small irregular lean, width and height changes; no spin, harsh distortion or global light change.
- Five small warm glow images and limited lantern tint modulation provide local light variation. Broad baked illumination remains original; no physical relighting or moving cast-shadow claim.
- One compact transform/color controller, one small nested Canvas to isolate dynamic geometry, no new raycaster, camera, Unity Light, particles, full-screen shader, postprocessing or gameplay system.
- Local repairs use built-in image_gen inpainting, adopted only under removal silhouettes plus feathered edge margin. Opaque native prop RGB is preserved. Every original pixel outside the final repair mask remains unchanged.
- Reference is a static image. Timing parameters are implementation tuning defaults, not timing measured from a supplied motion video.
- Visual gates: native rest assembly, contrasting-background edge QA, isolated Unity preview at 2400 × 1080, source integration, real Play Mode motion/interaction checks. Physical Android/A05s status is reported separately.
