# ASSETS_LIST.md — Liste des assets visuels

Inventaire exhaustif des assets nécessaires au projet. Statut à mettre
à jour au fil de la production : `à générer` → `généré` → `post-traité`
→ `intégré`.

---

## 1. Sprites scène

### Background

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hills_perche.png` | Nanobanana | post-traité | Paysage de collines vallonnées du Perche, 5 couches d'atmospheric perspective. Source détourée prête (3168×1344, alpha 32 bits). **Surveillance quantization** : préserver l'ordre tonal des crêtes (plus pâle au lointain, plus saturé au proche), une inversion ferait s'effondrer la profondeur. |

### Midground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `hedge_low_01.png` | Nanobanana | post-traité | Haie basse, variante 1. Source détourée prête. |
| `hedge_low_02.png` | Nanobanana | post-traité | Haie basse, variante 2 (variation visuelle). Source détourée prête. |
| `hedge_low_03.png` | Nanobanana | post-traité | **Variante DA ajoutée** (vs liste initiale qui n'en prévoyait que 2) pour enrichir la diversité visuelle quand le sprite sera tilé en scène. |
| `hedge_high_pollard_01.png` | Nanobanana | post-traité | Haie haute avec arbre têtard, variante 1. Source détourée prête. |
| `hedge_high_pollard_02.png` | Nanobanana | post-traité | Haie haute avec arbre têtard, variante 2. Source détourée prête. |
| `hedge_high_no_tree.png` | Nanobanana | post-traité | **Sprite ajouté DA** (non prévu liste initiale). Comble le linéaire de haie haute entre deux pollards (en bocage réel, pollards espacés tous les 8-15 m — éviter l'effet « pollard tous les 2 m » irréaliste lors du tiling). |
| `hedge_thin_sparse_01.png` | Nanobanana | post-traité | Haie en état **modérément dégradé** (haie encore continue, ~30 % moins dense que la saine, 1-2 troncs nus visibles), variante 1. Générée avec `hedge_low_01` comme seconde image de référence ip-adapter pour préserver la cohérence « même haie en moins bon état ». **Note sémantique** : malgré le nom de fichier `sparse`, ce n'est pas une dégradation extrême (premiers essais générant 3-5 fragments séparés ont été rejetés et archivés ailleurs). |
| `hedge_thin_sparse_02.png` | Nanobanana | post-traité | Idem variante 2 (alignée sur `hedge_low_02`). |
| `hedge_thin_sparse_03.png` | Nanobanana | post-traité | Idem variante 3 (alignée sur `hedge_low_03`). |

### Foreground

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `pollard_ash_main.png` | Nanobanana | post-traité | Arbre têtard de frêne, élément iconique, premier plan. Source détourée prête. **Note résolution** : sortie en 1024×651, sensiblement plus petite que les autres sprites foreground (~2.5-2.8 K de large). Pas bloquant — `postprocess.py` resize au longest-side cible — mais à confirmer côté DA si le détail est suffisant à l'échelle scène finale. |
| `pond.png` | Nanobanana | post-traité | Mare avec bords, sprite avec zone d'eau modulable par shader. Source détourée prête (2816×1536, alpha 32 bits). |
| `grass_border.png` | Nanobanana | post-traité | Bordure de prairie premier plan. Source détourée prête (2568×1632, alpha 32 bits). |

### Fauna

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `swallow_sheet.png` | Nanobanana wave 2 (frames 02/03/04) + `build_animation_sheet.py` | intégré 2026-05-30 | Hirondelle en vol, sprite sheet 3 frames horizontale (sub 256×143, sheet 768×143). Le legacy `bird_swallow_flight_v1` n'est pas inclus comme frame_01 (bbox 2110×1105 vs siblings ~2748×1536 → variation visible de taille du sujet). Cf §8. |
| `owl_sheet.png` | Nanobanana wave 2 (frames 02/03/04) + `build_animation_sheet.py` | intégré 2026-05-30 | Chouette chevêche en vol, sprite sheet 3 frames horizontale (sub 256×127, sheet 768×127). Legacy `bird_owl_flight_v1` initialement intégré comme frame_01 mais retiré 2026-05-30 (incohérence visuelle dessous des ailes plus clair sur frames wave 2 que sur legacy v1). Cf §8. |
| `buzzard_sheet.png` | Nanobanana wave 2 + `build_animation_sheet.py` | intégré 2026-05-30 | Buse variable (`Buteo buteo`) en glide planar, sprite sheet 3 frames horizontale (sub 256×130, sheet 768×130). **Remplace `bird_harrier_flight` rejeté** : l'ancien sprite était une mouette par erreur de prompt initial (correction ADR #49). Originaux archivés dans `Sprites/Source/_rejected/`. Cf §8. |
| `heron_sheet.png` | Nanobanana wave 1 (static) + wave 2 (alert) + `build_animation_sheet.py` | intégré 2026-05-30 | Héron cendré au bord de la mare, sprite sheet **2 frames** (sub 256×325, sheet 512×325) : frame 0 = pose de repos (`heron_static_v1`), frame 1 = tête tournée alerte (`heron_alert_v1`). Sentinelle biodiv (MotionMode StaticAppearance, apparaît ≥ 0.65, fade in/out) avec head-turn rare (≈ toutes les 18 s, maintenu 2.5 s). `heron_hunting_v1` livré en source mais **non intégré** (réservé post-MVP). Remplace l'ancien `heron.png` 1-frame supprimé. |
| `amphibian_small.png` | Nanobanana | **non produit, écarté** | Sacrifié comme prévu (statut optionnel/coupable). La lecture biodiversité est portée par les 4 autres sprites faune (hirondelle, chouette, buse, héron). Décision DA 2026-05-12. |

### Sensors (visibles dans la scène)

| Nom | Source | Statut | Notes |
|---|---|---|---|
| `weather_station.png` | Nanobanana | post-traité | Station météo, mât avec capteurs. Source détourée prête (2568×1632, alpha 32 bits). Bouclier solaire en blanc cassé pâle. |
| `piezometer.png` | Nanobanana | post-traité | Piézomètre, tube de mesure de nappe. Source détourée prête (2572×1632, alpha 32 bits). **Surveillance quantization** : léger modelé 3D résiduel sur le tube ; la quantization doit l'écraser. Si elle ne le fait pas et que le modelé persiste de façon visible, retouche manuelle. |
| `acoustic_sensor.png` | Nanobanana | post-traité | Capteur acoustique, micro directionnel. Source détourée prête (1023×651, alpha 32 bits). **Note résolution** : sortie en 1023×651, sensiblement plus petite que les autres capteurs (~2.5 K de large). Cf. note `pollard_ash_main`. |
| `photo_trap.png` | Nanobanana | post-traité | Piège photo, **sprite standalone sans support** (écart vs. spec initiale « boîtier sur tronc »). Choix DA assumé pour : (a) éviter conflit visuel avec troncs existants, (b) flexibilité de placement à l'intégration, (c) cohérence avec les autres capteurs standalone. À l'intégration Unity, positionner sur un élément porteur (tronc de pollard ou piquet implicite). Source détourée prête (2043×1644 après crop manuel, alpha 32 bits). Rendu en perspective axonométrique 3/4 face (3 tons de brun), unique parmi les capteurs en silhouette frontale. **Surveillance quantization** : la palette devrait réduire la boîte à 2 tons (base + ombre) pour atténuer l'effet 3D. |
| `eddy_covariance_tower.png` | Nanobanana | **post-traité (MVP : détourage à refaire en v2)** | Tour eddy covariance, treillis ouvert. Source détourée présente (2572×1632, alpha 32 bits) mais **contamination magenta résiduelle dans le treillis** (~20% des pixels opaques en halos rose-magenta `#9D5083`-ish, soit ~26 700 px sur 133 K). Le détourage manuel n'a pas utilisé une tolérance baguette assez élevée (cible 70-80). **Décision DA 2026-05-12** : accepter en l'état pour le MVP — la quantization écrase les halos sur un gris-rosé palette qui reste lisible. Re-détourage planifié post-MVP. La tour reste exclue de l'extraction palette `v1.0` (sinon le centroïde rose-magenta pollue la palette). |

---

## 2. Icônes UI

Source : **Lucide Icons** (https://lucide.dev), import direct, pas de
génération IA. Licence ISC, libre d'usage.

### Contrôles temps

- `Play`
- `Pause`
- `FastForward`
- `SkipForward`

### Climat

- `Droplet`
- `Thermometer`
- `Wind`
- `Sun`

### Biodiversité

- `Bird`
- `Bug`
- `Sprout`
- `TreePine`

### Économie

- `TrendingUp`
- `Coins`
- `Calculator`

### Événements

- `AlertTriangle`
- `Bell`
- `Info`

**Statut global** : à intégrer (téléchargement direct depuis
lucide.dev, format SVG converti en sprite Unity).

---

## 3. Plan minimap

Source : **fait main** (Inkscape ou Figma), dessin vectoriel exporté
en SVG ou PNG haute résolution.

| Élément | Description |
|---|---|
| Contour de parcelle | Ligne fermée représentant les limites du site. |
| Haies | Lignes fines, variations d'épaisseur selon densité. |
| Mare | Cercle / forme libre. |
| Bosquet | Zone teintée. |
| Arbre têtard | Symbole ponctuel (cercle plein). |
| Chemins | Lignes pointillées. |
| Capteurs | Symboles géométriques distincts par type (cercle, carré, triangle). |

**Statut** : à dessiner.

**Responsable** : utilisateur (cf `CLAUDE.md` §2 — division du travail).

---

## 4. Particules Unity

Configurées dans Unity, pas d'asset externe.

| Effet | Notes |
|---|---|
| Feuilles dérivantes au vent | Particle System, sprite simple, modulé par variable météo (force du vent). |
| Poussières dans la lumière | Particle System ambient, modulation densité par variable d'humidité. |

**Statut** : à configurer dans Unity (étape 9).

---

## 5. Polices

À télécharger et placer dans `Assets/_Project/Fonts/`.

| Police | Source | Licence | Usage |
|---|---|---|---|
| **EB Garamond** | Google Fonts | OFL (Open Font License) | Titres, labels, tooltips italique. |
| **JetBrains Mono** | jetbrains.com/lp/mono | Apache 2.0 | Valeurs chiffrées. |
| **IBM Plex Mono** (alternative) | Google Fonts | OFL | Alternative à JetBrains Mono. |

**Étape supplémentaire** : générer les assets TMP SDF via
`Window > TextMeshPro > Font Asset Creator`. Atlas 1024×1024, inclure
caractères latins étendus (accents français, €).

**Statut** : à télécharger et convertir.

---

## 6. Pipeline de génération IA

### Style guide visuel

- **Prompt template** : à définir et conserver dans
  `Sprites/Source/PROMPT_TEMPLATE.md` (créé à l'étape 9 si non déjà
  établi).
- **Image de référence stylistique** : à générer en premier, sert de
  référence ip-adapter pour tous les sprites suivants.
  - Nom : `style_reference.png`
  - Stockage : `Sprites/Source/style_reference.png`
  - Critères : esprit Charles Harper + chaleur A Short Hike +
    palette colombages percherons (cf `DECISIONS.md` #3).

### Étapes pour chaque sprite

1. Génération sur Nanobanana avec ip-adapter pointant sur l'image-ancre
   stylistique (`Sprites/Source/01_anchor_full_scene.png`).
2. Sortie brute archivée dans `Sprites/Source/<name>_v<n>.png`
   (hors `Assets/`, racine du repo, pour ne pas alourdir l'import
   Unity ni le hash de cache CI).
3. Détourage manuel par l'utilisateur (Photoshop / GIMP) ; sortie
   archivée à côté avec suffixe `_detoured.png`.
4. Post-traitement automatique via `python tools/postprocess.py
   <source>_detoured.png <destination>.png` :
   - **Chroma cleanup** (étape 1, ajoutée 2026-05-12) : force alpha à 0
     sur les pixels strictement magenta pur (R>240, G<30, B>240) ou vert
     pur (R<30, G>240, B<30). Filet de sécurité pour les sprites où le
     détourage manuel aurait laissé des pixels chroma-key isolés
     (typiquement les sujets à treillis ouvert). Ne sauve PAS la color
     decontamination (pixels en anti-aliasing semi-magenta tintés
     rose-brun) — pour ces cas, c'est le détourage utilisateur qui doit
     être correct dès le départ (wand tolerance 70-80).
   - Alpha cleanup (snap < 30 → 0, > 230 → 255, conserve les bords
     anti-aliasés).
   - Palette quantization sur la palette Perche (`tools/palette_perche.json`).
   - Crop au bounding box alpha + resize au longest-side cible
     (`--max-size`, défaut 512 px).
5. Export dans le sous-dossier thématique
   (`Background/`, `Midground/`, `Foreground/`, `Fauna/`, `Sensors/`).
   Pour un run multi-sprites, utiliser `python tools/bulk_quantize.py`
   qui orchestre l'appel à `postprocess.py` pour chaque sprite avec sa
   destination canonique et son `--max-size` catégoriel. Le mapping
   complet (source → destination → taille cible) est la source de
   vérité dans la table `SPRITES` de ce script.

   **Cas spécial — familles de frames animées** (livré 2026-05-30) :
   pour les sprites multi-frames (sheets animées comme la faune en
   vol), utiliser `python tools/build_animation_sheet.py <famille>
   <output_sheet> <frames…>` qui ajoute deux étapes au pipeline
   standard : (a) **alignement cross-frame** par alpha-bbox commun
   (le sujet reste à la même taille et position relative dans chaque
   frame, ce qui corrige les détourages au cadrage incohérent) ; (b)
   **concaténation horizontale** + génération du `.meta` Unity avec
   `spriteMode: 2` (Multiple), rects grid pré-écrits, `filterMode: 1`
   (Bilinear) et GUIDs déterministes (stables aux re-runs). Réutilise
   `chroma_key_removal`, `alpha_cleanup`, `quantize_to_palette` de
   `postprocess.py`. Cf §8 pour les 3 familles faune (swallow / owl /
   buzzard) livrées via cet outil.
6. Validation visuelle DA avant intégration.
7. **Configuration d'import Unity — Crunch compression sur l'override
   Web** : optionnel. Unity ne l'active pas par défaut, c'est un
   réglage par-sprite.
   - Sélectionne le PNG dans le Project window.
   - Inspector → onglet plateforme **Web** (icône globe HTML5, à droite
     des onglets Default / Standalone / Android).
   - Coche **Override for Web**.
   - **Format** : `DXT1 Crunched` (sprite opaque) ou `DXT5 Crunched`
     (sprite avec alpha).
   - **Compressor Quality** : 50 (équilibre taille / artefact ; descend
     à 30 pour les sprites de fond peu détaillés, monte à 70 pour les
     sprites pixel-art ou à gradient fin).
   - **Apply**.
   - Vérifie visuellement qu'aucun artefact (bandes de couleur,
     aplats moirés) n'apparaît dans la Scene view. Si oui, soit monte
     la quality, soit désactive le Crunch uniquement sur ce sprite.

   Cette étape divise la taille DL des textures par 3 à 4 dans le
   build WebGL final.

   **Stratégie MVP courante (post-recadrage 2026-05-28)** : Crunch
   DXT5 conditionnel (chantier E7 de `docs/ROADMAP.md`). On mesure
   d'abord la taille du build et le TTI à l'issue des chantiers
   E1-E6. Si build ≤ 30 MB et TTI ≤ 10 s, on skip le Crunch. Si
   build > 30 MB ou TTI > 10 s, application du Crunch sur les sprites
   les plus lourds en priorité (paysage > UI > faune).

### Palette Perche

Définie dans `tools/palette_perche.json`. Statut : **`v1.0` (candidat,
en attente de validation DA finale)** au 2026-05-12.

**Méthode** : k-means à 32 couleurs (RGB), sous-échantillonnage
équilibré à 25 000 pixels opaques par sprite source (alpha ≥ 200) pour
que chaque sprite contribue à parts égales — un gros sprite haie
(2-3 MP) n'écrase pas les signatures chromatiques rares mais
fonctionnellement critiques des petits sprites (anneau métallique du
piézomètre, charcoal de la lentille piège photo, etc.). Reproductible
via :

```
python tools/extract_palette.py --sources-glob "Sprites/Source/*_detoured.png" \
    --exclude "Sprites/Source/01_anchor_full_scene.png" \
    --exclude "Sprites/Source/eddy_covariance_tower_v1_detoured.png" \
    --colors 32 --version-tag v1.0
```

**Exclusions** :

1. `01_anchor_full_scene.png` — contient aussi les couleurs UI du
   mockup (anthracite, ivoire) qui ne doivent pas polluer la palette
   scène.
2. `eddy_covariance_tower_v1_detoured.png` — contamination magenta
   résiduelle dans le treillis (~20 % de pixels opaques) due à un
   détourage à tolérance baguette insuffisante. À ré-intégrer dans la
   palette après re-détourage côté DA.

**Arbitrages DA 2026-05-12 (acceptés pour le MVP)** :

- **Joncs autour de la mare en vert saturé** (`pond_v1_detoured.png`) :
  ~58 K pixels en `#347C2F`-ish forment un centroïde palette (~954 px,
  la couleur la moins populée mais présente). La spec initiale demandait
  une fusion sur l'olive sourd des haies. **Décision DA : accepter
  (Option A)** — les joncs conservent leur teinte saturée. À
  re-évaluer post-MVP si nécessaire.
- **Absence du cream-ivoire chaud** (`#E8DDC4`) : la palette `v1.0`
  contient `#DFE1DF` (cool white) et `#C9A27D` (warm beige) mais pas
  exactement le ton ambré débattu en DA pour le ventre d'hirondelle.
  **Décision DA : accepter** — `swallow.png` est déjà intégré avec la
  `v0.1-provisional` et conserve son ton ; aucun autre sprite du corpus
  n'a clairement besoin de cette nuance pour le MVP.
- **Tour eddy covariance** : halos rose-magenta dans le treillis acceptés
  en l'état pour le MVP (re-détourage post-MVP). Cf. ligne dédiée table
  Sensors.

**Sprite déjà passé sur la palette `v0.1-provisional`** : `swallow.png`
(intégré). Peut être re-quantizé sur la `v1.0` après validation, mais
non bloquant.

---

## 7. Stratégie en cas d'échec génération IA

Hiérarchie des solutions (du plus acceptable au plus douloureux) :

1. **Réduire la complexité du prompt** (simplifier la description,
   réduire le nombre de détails attendus).
2. **Réduire le nombre de variantes** (passer de 2 variantes de haie
   basse à 1).
3. **Fusionner avec un autre sprite** (réutiliser un sprite voisin avec
   variation par shader).
4. **En tout dernier recours** : utiliser des assets libres
   (Kenney.nl, OpenGameArt) puis appliquer le post-traitement Python
   pour les rendre cohérents avec la palette Perche.

**Note** : ne jamais mélanger sprites IA et sprites externes sans
post-traitement uniformisant — la cohérence visuelle est un critère
non-négociable de l'étape 9.

---

## 8. Sprites faune — état post-vague 2 (2026-05-30)

Vague 2 livrée et intégrée. Trois sprite sheets animées sliced + un
héron statique dans `Assets/_Project/05_Presentation/Scene/Sprites/Fauna/`.
Sources brutes wave 2 archivées dans `Sprites/Source/`.

### 8.1 Inventaire intégré

| Espèce | Asset Unity | Sheet (W×H) | Frames | Sub-sprite | GUID |
|---|---|---|---|---|---|
| Hirondelle (`swallow`) | `swallow_sheet.png` | 768×143 | 3 | 256×143 | `57e4022c4bcf39240b7b84066820c15b` |
| Chouette chevêche (`owl`) | `owl_sheet.png` | 768×127 | 3 | 256×127 | `493298ebbd52e083e617833714552e12` |
| Buse variable (`buzzard`) | `buzzard_sheet.png` | 768×130 | 3 | 256×130 | `33c2f60ec470881371bfb4f999d40830` |
| Héron cendré (`heron`) | `heron_sheet.png` | 512×325 | 2 | 256×325 | `b365893e732f78262a9d009826a73860` |

**Configuration import Unity** (toute la faune) :

- `textureType: 8` (Sprite 2D and UI)
- `spriteMode: 2` (Multiple) pour les sheets ; les rects sub-sprite
  sont écrits par `build_animation_sheet.py`, pas besoin de slicer
  dans l'éditeur Unity.
- `spritePixelsToUnits: 100`
- `filterMode: 1` (**Bilinear** — passage de Point à Bilinear le
  2026-05-30 pour rendu lisse, sprites non pixel-art).
- 3 `platformSettings` (DefaultTexturePlatform / Standalone / WebGL).
- `crunchedCompression: 0` (chantier E7 conditionnel, cf §6 étape 7).

### 8.2 Décisions techniques notables

- **Buse variable remplace busard Saint-Martin** (correction critique
  2026-05-30, ADR #49). Le sprite `bird_harrier_flight_v1` originel
  était une mouette par erreur de prompt initial. Archivé dans
  `Sprites/Source/_rejected/`. La buse a été re-générée et nommée
  `buzzard` partout (asset Unity, sheet, GUIDs, sub-sprite names,
  futur `FaunaSpecies_Buzzard.asset` en E4 code).
- **Hirondelle 3-frame** (pas 4). Le legacy `bird_swallow_flight_v1`
  détouré présente un bbox (2110×1105) significativement plus petit
  que les frames wave 2 (~2748×1536), ce qui produirait une variation
  de taille du sujet ≈ 25 % entre frames. Décision 2026-05-30 : drop
  le legacy v1, animation à 3 frames sur les wave 2 (02/03/04
  renumérotés 01/02/03 côté sub-sprite). Si une 4ᵉ frame est
  réintégrée plus tard, le legacy v1 devra être re-détouré au canvas
  commun.
- **Chouette 3-frame** (pas 4). Tentative initiale avec legacy
  `bird_owl_flight_v1` comme frame_01 : les dimensions étaient
  compatibles (2848×1490 ≈ wave 2 2852×1472) mais la couleur du
  dessous des ailes diverge (legacy plus sombre, wave 2 plus clair)
  — incohérence visible pendant le cycle wing-flap, repérée à
  validation visuelle utilisateur 2026-05-30. Décision : drop legacy
  v1, animation 3-frame sur wave 2 (02/03/04 renumérotés 01/02/03),
  même pattern que le swallow. Leçon : la compatibilité dimensions
  est nécessaire mais pas suffisante — la cohérence chromatique entre
  vagues de génération doit aussi être validée à l'œil.
- **Héron sentinelle avec head-turn** (décision révisée 2026-05-30).
  Initialement prévu statique 1-frame, finalement livré en sheet
  2-frames (`heron_static_v1` repos + `heron_alert_v1` tête tournée).
  Le héron est un indicateur de bonne santé écologique : il apparaît
  (fade in) quand la biodiv composite ≥ 0.65 et disparaît (fade out)
  sinon — MotionMode `StaticAppearance`, pas de traversée. De temps en
  temps (Poisson, ≈ 1 fois / 18 s, pose maintenue 2.5 s) il tourne la
  tête (swap frame alert) avant de revenir au repos. `heron_hunting_v1`
  livré en source mais **non intégré** (réservé post-MVP — la pose de
  pêche n'a pas de déclencheur mesuré honnête pour l'instant). L'ancien
  `heron.png` 1-frame est supprimé.

### 8.3 Pipeline étendu — `tools/build_animation_sheet.py`

`tools/postprocess.py` reste single-image-in/out (crop bbox + resize
indépendant par image), ce qui ne garantit pas l'alignement
frame-à-frame nécessaire à une animation. Nouveau outil
`tools/build_animation_sheet.py` (livré 2026-05-30, cf §6) :

1. Charge N frames d'une famille (PNG détourées).
2. Applique `chroma_key_removal` + `alpha_cleanup` (importés depuis
   `postprocess.py`).
3. Calcule la bounding box alpha de chaque frame, retient la max
   dimension (width et height) sur toute la famille, ajoute marge 5%
   → canvas commun.
4. Crop chaque frame à son propre bbox, paste centrée dans le canvas
   commun (le sujet reste à la même position visuelle entre frames).
5. Quantize chaque frame sur la palette Perche
   (`tools/palette_perche.json`).
6. Redimensionne chaque frame à `--max-subsprite-width` (défaut
   256 px) en preservant le ratio.
7. Concatène horizontalement → sprite sheet (sheet_width = N × sub_w).
8. Écrit `<famille>_sheet.png` + `<famille>_sheet.png.meta` avec
   GUID déterministe (hash stable du nom de famille → re-runs
   idempotents), `spriteMode: 2`, rects grid pré-écrits,
   `filterMode: 1`, 3 platformSettings, `nameFileIdTable` cohérente.

### 8.4 Caveats connus (informationnels, non bloquants)

- **`uniq_rgb` par sub-sprite ≈ 1000-2000** après le pipeline.
  Origine : après quantization sur 32 couleurs palette Perche,
  l'étape de resize LANCZOS interpole entre couleurs palette aux
  bords AA. C'est le comportement souhaité pour un rendu lisse
  cohérent avec `filterMode: 1` (Bilinear) — pas du pixel art.
  Vérification distance-palette : 100 % des pixels opaques restent
  à distance RGB ≤ 40 d'une couleur palette ; ~52 % sont
  exact-match palette ; ~85 % à distance ≤ 10.
- **Résidu chroma résiduel** : ≤ 5 pixels par sub-sprite (soit
  ≤ 0.06 % des pixels opaques) ont une couleur RGB pure magenta ou
  pure verte avec **alpha = 1 ou 2** (invisibles). Origine :
  `chroma_key_removal` met l'alpha à 0 sur les pixels chroma purs
  avant resize, mais LANCZOS interpole alpha et RGB séparément, ce
  qui peut faire ré-émerger des micro-fragments chroma à alpha
  quasi-nul aux bords. Aucun impact visuel ni runtime. Amélioration
  possible (non bloquante) : ré-appliquer `chroma_key_removal`
  après resize dans `build_animation_sheet.py`.

### 8.5 Charge utilisateur restante

Aucune pour la faune MVP. Les 4 espèces sont intégrées. Les variantes
héron alert/hunting et toute frame supplémentaire (e.g. swallow
frame_01 re-détouré) sont à planifier hors-MVP via `FaunaSpecies_*`
ScriptableObjects à étendre en E4 code, avec sources livrées en wave
ultérieure.
