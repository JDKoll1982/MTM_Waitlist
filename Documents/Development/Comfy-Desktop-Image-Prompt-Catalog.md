# MTM Waitlist Image Prompt Catalog

This catalog maps the current application surfaces to image opportunities and provides Comfy Desktop prompts for the workflow shown in the supplied screenshots.

The visual direction is inspired by the public Manitowoc Tool & Manufacturing site: precision metal stamping, custom fabrication, tool-and-die work, robotic production, laser-cut steel, and a large organized manufacturing facility. These prompts are original application artwork prompts; they do not reproduce MTM branding, logos, website layouts, or copyrighted photographs.

## Comfy Desktop Workflow

Use the existing Text to Image (Z-Image-Turbo) workflow:

- `vae_name`: `ae.safetensors`
- `clip_name`: `qwen_3_4b.safetensors`
- `unet_name`: `z_image_turbo_bf16.safetensors`
- `width`: `512`
- `height`: `512`
- `steps`: `8`
- `seed`: use a fixed seed while iterating, then randomize for final variants
- Save each output as a PNG and rename it to the target asset name below.

The current application uses square PNGs with a light background for request cards. Keep the subject centered with generous margins so it remains recognizable inside the 96px card image and the 110px detail hero.

## Unified MTM Visual Theme

Use this style direction in every positive prompt, including the request objects and the wider splash/login images:

```text
original documentary industrial photography inspired by a precision metal stamping and custom fabrication facility, clean organized production environment, steel gray and brushed-silver materials, restrained cobalt-blue structure, safety-orange and signal-green accents, crisp high-key facility lighting, realistic manufactured surfaces, practical engineering detail, professional enterprise software artwork, no visible branding
```

The images should feel like they belong to one facility and one operations system. Use the same cool neutral white/steel-gray background, the same restrained accent colors, the same clean lighting, and the same realistic photographic finish. Do not mix glossy advertising renders, cartoons, flat vector art, dark cinematic scenes, or unrelated warehouse stock photography.

### Theme Lock Suffix

Append this exact suffix to every positive prompt after the asset-specific description when iterating in Comfy Desktop:

```text
same MTM Waitlist visual system, same precision-fabrication facility, same brushed-steel and steel-gray material language, same restrained cobalt-blue, safety-orange, and signal-green accents, same crisp high-key documentary industrial photography, centered readable silhouette, no visible branding
```

For the six square request assets, keep the camera and lighting consistent across seeds: eye-level three-quarter product view, 50mm equivalent lens, soft overhead facility lighting, clean steel-gray floor, subject inside the central 70 percent of the frame. For splash and login assets, use the same facility and palette with a wider 24mm equivalent view.

## Shared Negative Prompt

Use this negative prompt for every generated asset unless a section below supplies an override:

```text
text, words, letters, labels, logos, brand marks, copied branding, watermark, signature, people in focus, hands, faces, cartoon, mascot, flat vector icon, UI screenshot, collage, multiple unrelated objects, duplicate object, cropped object, cut off object, tilted framing, extreme perspective, clutter, busy background, dark moody lighting, dramatic shadows, harsh reflections, motion blur, depth of field blur, low resolution, noisy image, grain, jpeg artifacts, oversaturated colors, neon colors, purple dominance, malformed geometry, impossible parts, broken edges, fake text
```

## Existing Request Card Assets

These six images are currently consumed by `SampleOrder.ImagePath`, `WaitlistLineCardView`, and `WaitlistViewDetailPage`.

### `Assets/coil.png`

**Used by:** Coil request line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision metal stamping and fabrication facility, single galvanized steel coil, tightly wound sheet metal roll, visible circular inner core, brushed silver surface, black retaining strap, subtle cobalt-blue fixture and small safety-orange accent in the background, three-quarter product view, centered, isolated on a clean steel-gray studio floor, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, people, hands, faces, multiple coils, uncoiled sheet, rusty metal, damaged coil, forklift, factory background, pallet, cropped object, extreme perspective, clutter, busy background, harsh reflections, motion blur, low resolution, noisy image, malformed geometry
```

### `Assets/pickup_fg.png`

**Used by:** Finished Goods pickup line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision metal stamping and value-added assembly facility, single finished-goods material tote on a clean steel pallet, signal-green reusable container, subtle brushed-steel rails visible inside, small cobalt-blue fixture and safety-orange marker, three-quarter product view, centered on a clean steel-gray production floor, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, FG lettering, labels, logos, watermark, signature, people, hands, faces, multiple totes, open cardboard box, trash, scrap, forklift, warehouse background, cropped object, tilted framing, clutter, busy background, harsh shadows, neon green, cartoon, low resolution, malformed geometry
```

### `Assets/pickup_ncm.png`

**Used by:** NCM pickup line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision metal stamping quality-inspection area, single safety-orange nonconforming-material steel container on a clean steel pallet, a few neutral gray stamped metal pieces inside, cobalt-blue inspection fixture, signal-green status accent, clear safety-oriented industrial shape, three-quarter product view, centered on a clean steel-gray production floor, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, NCM lettering, labels, logos, watermark, signature, people, hands, faces, multiple bins, hazardous spill, fire, blood, dangerous scene, forklift, factory background, cropped object, clutter, busy background, harsh shadows, neon orange, cartoon, low resolution, malformed geometry
```

### `Assets/pickup_os.png`

**Used by:** Outside Service pickup line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision fabrication and logistics area, single dark steel outside-service shipment crate on a clean steel pallet, durable manufactured container, restrained safety-orange corner guard and cobalt-blue handling fixture, clean organized production floor, three-quarter product view, centered, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, people, hands, faces, multiple crates, shipping labels, barcode, truck, trailer, warehouse background, rust, damage, clutter, busy background, cropped object, extreme perspective, harsh reflections, cartoon, low resolution, malformed geometry
```

### `Assets/pickup_wip.png`

**Used by:** WIP pickup line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a robotic metal-stamping production line, single cobalt-blue work-in-process tote on a clean steel pallet, a few neutral stamped metal parts inside, brushed-silver tooling and a restrained safety-orange guard in the background, practical organized factory-floor object, three-quarter product view, centered, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, WIP lettering, labels, logos, watermark, signature, people, hands, faces, multiple totes, finished retail packaging, scrap, forklift, warehouse background, clutter, busy background, cropped object, harsh shadows, neon blue, cartoon, low resolution, malformed geometry
```

### `Assets/scrap.png`

**Used by:** Scrap pickup line card and detail hero.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision stamping and fabrication floor, single dark steel scrap lugger with a low hopper body, a small amount of clean reflective metal offcuts inside, sturdy wheeled manufacturing container, restrained safety-orange handle and signal-green floor marking, organized steel-gray production environment, three-quarter product view, centered, crisp high-key facility lighting, realistic manufactured surfaces, square composition, generous whitespace, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, people, hands, faces, multiple luggers, overflowing garbage, dangerous sharp debris, fire, smoke, forklift, warehouse background, rust, blood, clutter, busy background, cropped object, harsh shadows, cartoon, low resolution, malformed geometry
```

## New Image Opportunities

### 1. Splash / startup visual

**Current surface:** `Module_Startup/Views/SplashView.xaml` and `SplashPage.xaml` use a progress ring and text only.

**Suggested asset:** `Assets/mtm-waitlist-startup.png`

Use this as a restrained visual beside or behind the startup status, not as a full-screen marketing hero. It should communicate manufacturing flow and waitlist coordination without competing with the progress state.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision metal stamping and custom fabrication facility, wide organized production aisle, rows of steel coils, robotic stamping equipment, brushed-silver tool-and-die fixtures, labeled-free industrial containers, clean material-handling lane leading toward a bright production floor, steel gray and cobalt-blue structure with restrained safety-orange and signal-green accents, crisp high-key facility lighting, calm reliable operations atmosphere, clear negative space in the center for application status text, wide landscape composition, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, close-up machinery, dramatic smoke, sparks, fire, accidents, cluttered factory, messy floor, people in focus, faces, dark horror lighting, neon colors, excessive contrast, blur, shallow depth of field, cropped composition, UI screenshot, advertisement, low resolution
```

### 2. Login visual

**Current surface:** `Module_Startup/Views/LoginPage.xaml` is a centered credential card with no supporting image.

**Suggested asset:** `Assets/mtm-waitlist-login.png`

Use as a small side panel or a compact top visual. Keep the credential card readable and do not place text generated by the model in the image.

**Positive prompt:**

```text
original documentary industrial photography inspired by precision metal stamping, robotic fabrication, and value-added assembly, steel coil, signal-green material tote, and safety-orange quality container arranged on one clean brushed-steel work surface, cobalt-blue tooling fixture behind them, coherent single-facility visual language, balanced three-object composition, cool steel-gray background, crisp high-key facility lighting, trustworthy professional enterprise visual, realistic manufactured surfaces, wide landscape composition, generous empty space, no people, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, people, hands, faces, unsafe work practices, clutter, dirty floor, heavy smoke, sparks, fire, dramatic cinematic poster, dark moody scene, neon colors, cartoon, collage, duplicate objects, cropped objects, blur, low resolution
```

### 3. Empty waitlist state

**Current surface:** `Module_Waitlist/Views/WaitlistViewPage.xaml` has a `ListView` and add button but no dedicated empty-state visual.

**Suggested asset:** `Assets/waitlist-empty.png`

Display only when `Source` is empty. Prefer a small centered image above the empty-state text.

**Positive prompt:**

```text
original documentary industrial photography inspired by an organized precision-fabrication facility, single empty steel-gray staging bay with one clean cobalt-blue material tote waiting on a marked floor position, brushed-silver safety rail, one restrained signal-green status light and small safety-orange floor marker, calm organized manufacturing environment, crisp high-key facility lighting, centered subject, uncluttered background, square composition, generous whitespace, no people, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, full warehouse panorama, people, faces, crowded shelves, spilled material, damaged equipment, warning scene, red emergency lighting, dark shadows, neon colors, cartoon, icon, flat vector, blur, low resolution, cropped object
```

### 4. Database failure / recovery state

**Current surface:** `Module_Startup/Views/SplashView.xaml` shows a critical red banner when the database is unavailable.

**Suggested asset:** `Assets/database-recovery.png`

Use as a small illustration beside the failure message. It should support recovery actions without making the failure feel alarming.

**Positive prompt:**

```text
original documentary industrial photography inspired by a precision manufacturing control station, clean brushed-steel workstation with an intact network cable slightly disconnected beside a small status light transitioning from safety-orange to signal-green, subtle cobalt-blue robotic production equipment softly visible in the background, calm troubleshooting concept, crisp high-key facility lighting, professional enterprise support visual, realistic manufactured surfaces, centered composition, steel-gray background, square image, no people, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, labels, logos, watermark, signature, people, hands, faces, broken computer, explosion, fire, panic, danger, red flashing emergency lights, server room maze, cyberpunk, neon colors, hacker imagery, cartoon, UI screenshot, clutter, blur, low resolution
```

### 5. Settings about card

**Current surface:** `Module_Settings/Views/SettingsPage.xaml` uses a `ContactInfo` icon and text-only About card.

**Suggested asset:** `Assets/mtm-waitlist-mark.png`

This can be a small product mark or manufacturing motif in the About section. Keep the existing `SymbolIcon` for fast scanning; use the image only as supporting identity.

**Positive prompt:**

```text
original minimal professional manufacturing operations emblem, abstract brushed-steel coil curve intersecting with three orderly material containers and a precise tool-and-die geometry, steel gray and cobalt blue foundation with small safety-orange and signal-green accents, clean geometric industrial identity mark inspired by precision stamping and fabrication, centered on a white background, crisp edges, simple dimensional product identity artwork, square composition, no visible branding, no text
```

**Negative prompt:**

```text
text, words, letters, initials, watermark, signature, copied brand logo, complex lettering, gradients with banding, glossy 3d mascot, people, faces, factory panorama, clutter, busy background, purple dominance, neon colors, blur, low resolution, asymmetry, malformed geometry
```

## Areas That Should Stay Icon-Only

These surfaces already benefit from native WinUI icons and should not receive generated raster artwork:

- Shell navigation item icons.
- Search `Find` icon.
- Edit, cancel, accept, and add actions.
- Role/privilege status badge icons.
- Settings theme, version, and contact icons.
- Progress ring and database failure action controls.

Native icons stay sharper, theme-aware, accessible, and easier to recognize than generated images at small sizes.

## Output and Integration Notes

- Generate at `512x512` for square request and state images; generate `1024x576` for splash/login landscape images if the workflow allows it.
- Use the same fixed seed for the first comparative pass across all six request assets, then make a second pass with controlled seed changes. This makes lighting and palette differences easier to compare.
- Keep the same sampler, VAE, CLIP, UNet, steps, and CFG settings for every asset. Change only the subject description, aspect ratio, and seed.
- Prefer PNG output. Keep the background consistent with the existing white-background request images unless the asset is intentionally placed over a themed panel.
- Do not rely on generated text. Add labels, badges, and request-type names in XAML.
- Keep the object inside the central 70 percent of the canvas for the current 96px line-card image and 110px detail image.
- Create two or three seed variants per asset and choose the one with the cleanest silhouette, fewest artifacts, and strongest readability at thumbnail size.
- Before replacing current assets, compare each candidate at 96px and 110px in the actual WinUI cards.
- Keep the existing filenames for request images if replacing them; the current model and selector logic already depend on those filenames.
- New splash, login, empty-state, recovery, and About assets should be added explicitly to `MTM_Waitlist.csproj` and referenced from the owning module's XAML through `ms-appx:///Assets/...`.
